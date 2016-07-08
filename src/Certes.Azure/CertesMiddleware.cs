using Certes.Acme;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Certes.Azure
{
    public class CertesMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger logger;
        private readonly CertesOptions options;
        private readonly ISslBindingManager bindingManager;
        private readonly IContextStore contextStore;
        private readonly IChallengeResponderFactory challengeResponderFactory;
        private readonly ICsrBuilderFactory csrBuilderFactory;

        public CertesMiddleware(
            RequestDelegate next,
            IOptions<CertesOptions> optionsAccessor,
            ILoggerFactory loggerFactory,
            IContextStore contextStore,
            IChallengeResponderFactory challengeResponderFactory,
            ICsrBuilderFactory csrBuilderFactory,
            ISslBindingManager bindingManager)
        {
            this.next = next;
            this.logger = loggerFactory.CreateLogger<CertesMiddleware>();
            this.options = optionsAccessor.Value;
            this.bindingManager = bindingManager;
            this.contextStore = contextStore;
            this.challengeResponderFactory = challengeResponderFactory;
            this.csrBuilderFactory = csrBuilderFactory;
        }

        public async Task Invoke(HttpContext context)
        {
            var bindingGroups = await CheckCertificates();
            if (bindingGroups.Count == 0)
            {
                // All host names have valid SSL
                return;
            }

            var account = await this.contextStore.GetAccount();

            using (var client = new AcmeClient(options.DirectoryUri))
            {
                if (account == null)
                {
                    account = await client.NewRegistraton(); // TODO: add optional contact method
                    await this.contextStore.SetAccount(account);
                }
                else
                {
                    client.Use(account.Key);
                }

                foreach (var bindingGroup in bindingGroups)
                {
                    var authzChallenges = await GetChallenges(client, bindingGroup);

                    if (authzChallenges.Any(c => c.Item2 == null))
                    {
                        // There's at least one authz we can not complete, discard all authz in current group
                        foreach (var authz in authzChallenges.Select(c => c.Item1))
                        {
                            await client.CompleteChallenge(authz.Data.Challenges.First());
                        }
                        
                        continue;
                    }
                    else
                    {
                        foreach (var authz in authzChallenges)
                        {
                            foreach (var challenge in authz.Item2)
                            {
                                // Make sure the key authz string is ready
                                challenge.KeyAuthorization = client.ComputeKeyAuthorization(challenge);

                                var responder = this.challengeResponderFactory.GetResponder(challenge.Type);
                                await responder.Deploy(challenge);
                            }
                        }

                        // TODO: Maybe an optional delay to avoid caching issues?
                        
                        foreach (var challenge in authzChallenges.SelectMany(a => a.Item2))
                        {
                            await client.CompleteChallenge(challenge);
                        }

                        var authzFailed = false;
                        var pendingAuthz = authzChallenges.Select(a => a.Item1).ToList();
                        while (pendingAuthz.Count > 0 && !authzFailed)
                        {
                            await Task.Delay(5000); // TODO: make configurable
                            var links = pendingAuthz.Select(a => a.Location);
                            pendingAuthz.Clear();
                            foreach (var link in links)
                            {
                                var authz = await client.GetAuthorization(link);
                                await this.contextStore.SetAuthorization(authz);
                                
                                if (authz.Data.Status == EntityStatus.Pending)
                                {

                                }
                                else if (authz.Data.Status != EntityStatus.Valid)
                                {
                                    authzFailed = true;
                                }
                            }
                        }

                        if (authzFailed)
                        {
                            // Failed, try next group
                            continue;
                        }

                    }

                    var hostNames = bindingGroup.Select(g => g.HostName).ToArray();

                    // Authorization done, start generating certificate
                    var csr = this.csrBuilderFactory.Create();
                    csr.AddName("CN", hostNames.First());
                    foreach (var altName in hostNames.Skip(1))
                    {
                        csr.SubjectAlternativeNames.Add(altName);
                    }

                    var cert = await client.NewCertificate(csr);

                    // TODO: check if the cert contains all the requested host names

                    var thumbprint = cert.GetThumbprint();
                    var password = Guid.NewGuid().ToString("N"); // TODO: make configurable
                    var pfx = cert.ToPfx().Build($"certes-{thumbprint}", password);
                    
                    await bindingManager.InstallCertificate(thumbprint, pfx, password);
                    await bindingManager.UpdateSslBindings(thumbprint, hostNames);
                }
            }
            await next.Invoke(context);
        }

        private async Task<List<Tuple<AcmeResult<Authorization>, Challenge[]>>> GetChallenges(
            AcmeClient client, IList<SslBinding> bindingGroup)
        {
            var authzChallenges = new List<Tuple<AcmeResult<Authorization>, Challenge[]>>();

            foreach (var binding in bindingGroup)
            {
                var id = new AuthorizationIdentifier
                {
                    Type = AuthorizationIdentifierTypes.Dns,
                    Value = binding.HostName
                };

                var authz = await this.contextStore.GetAuthorization(id);

                if (authz?.Data?.Status == EntityStatus.Pending)
                {
                    authz = await client.GetAuthorization(authz.Location);
                    await this.contextStore.SetAuthorization(authz);
                }

                if (authz?.Data?.Status == EntityStatus.Valid && authz?.Data?.Expires < DateTimeOffset.Now.AddHours(-1))
                {
                    // Host name has valid authz
                }
                else
                {
                    authz = await client.NewAuthorization(id);
                    await this.contextStore.SetAuthorization(authz);

                    var challenges = authz.Data.Combinations
                        .Where(combination => !combination
                            .Select(i => authz.Data.Challenges[i])
                            .Any(c => !this.challengeResponderFactory.IsSupported(c.Type)))
                        .Select(combination => combination.Select(i => authz.Data.Challenges[i]).ToArray())
                        .FirstOrDefault();

                    authzChallenges.Add(Tuple.Create(authz, challenges));
                }
            }

            return authzChallenges;
        }

        private async Task<IList<IList<SslBinding>>> CheckCertificates()
        {
            var hostNames = await bindingManager.GetHostNames();

            // TODO: Support grouping host names by user filters, and specific CN

            // Group host names so we don't hit the SAN limit
            var namePerCert = options.MaxSanPerCert + 1; // with common name
            var bindingGroup = hostNames
                .OrderBy(h => h.HostName) // The groups should not change for the same set of host names
                .Select((h, i) => new { Group = i / options.MaxSanPerCert, Binding = h })
                .GroupBy(g => g.Group, g => g.Binding);

            // Renew only if no cert, or the cert is about to expire
            var renewDate = DateTimeOffset.Now.Add(options.RenewBeforeExpire);
            bindingGroup = bindingGroup
                .Where(g => g.Any(b => b.CertificateThumbprint == null || b.CertificateExpires <= renewDate));

            return bindingGroup.Select(b => b.ToArray()).ToArray();
        }
    }

    public static class CertesExtensions
    {
        private const string WebJobFilePrefix = "Certes.Azure.Resources.WebJob.";

        public static IApplicationBuilder UseCertes(this IApplicationBuilder app)
        {
            app.Map("/.certes/renew", sub =>
            {
                sub.UseMiddleware<CertesMiddleware>();
            });

            return app;
        }

        public static IApplicationBuilder UseCertesChallengeHandler(this IApplicationBuilder app)
        {
            app.Map("/.well-known/acme-challenge", sub =>
            {
            });

            return app;
        }

        public static IApplicationBuilder UseCertesWebJob(this IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            var webJobPath = Path.Combine(env.ContentRootPath, "app_data/jobs/triggered/certes");

            var dir = new DirectoryInfo(webJobPath);
            if (!dir.Exists)
            {
                dir.Create();
            }

            var assembly = typeof(CertesMiddleware).GetTypeInfo().Assembly;
            var webJobFiles = assembly
                .GetManifestResourceNames()
                .Where(n => n.StartsWith(WebJobFilePrefix))
                .ToArray();

            foreach (var file in webJobFiles)
            {
                var filename = file.Substring(WebJobFilePrefix.Length);
                var dest = Path.Combine(webJobPath, filename);
                using (var destStream = File.Create(dest))
                using (var srcStream = assembly.GetManifestResourceStream(file))
                {
                    srcStream.CopyTo(destStream);
                }
            }

            return app;
        }
    }
}
