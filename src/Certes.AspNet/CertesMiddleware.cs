using Certes.Acme;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.AspNet
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
            logger.LogDebug("Start renewing SSL certificates.");

            var bindingGroups = await CheckCertificates();
            if (bindingGroups.Count == 0)
            {
                // All host names have valid SSL
                logger.LogDebug("All host names have valid SSL bindings.");

                return;
            }

            var account = await this.contextStore.GetAccount();

            using (var client = new AcmeClient(options.DirectoryUri))
            {
                logger.LogDebug("Using ACME server {0}.", options.DirectoryUri);

                if (account == null)
                {
                    logger.LogDebug("No ACME account configured, register new account.");
                    account = await client.NewRegistraton(); // TODO: add optional contact method
                    account.Data.Agreement = account.GetTermsOfServiceUri();
                    await client.UpdateRegistration(account);
                    await this.contextStore.SetAccount(account);
                }
                else
                {
                    logger.LogDebug("Using existing ACME account.");
                    client.Use(account.Key);
                }

                logger.LogDebug("Renewing {0} certificates.", bindingGroups.Count);
                foreach (var bindingGroup in bindingGroups)
                {
                    logger.LogDebug("Start renewing certificate.");
                    var authzChallenges = await GetChallenges(client, bindingGroup);

                    if (authzChallenges.Any(c => c.Item2 == null))
                    {
                        logger.LogDebug("Discard pending authorizations.");
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

                                logger.LogDebug("Deploy {0} responder for {1}.", challenge.Type, authz.Item1.Data.Identifier.Value);
                                var responder = await this.challengeResponderFactory.GetResponder(challenge.Type);
                                await responder.Deploy(challenge);
                            }
                        }

                        // TODO: Maybe an optional delay to avoid caching issues?
                        
                        foreach (var authz in authzChallenges)
                        {
                            foreach (var challenge in authz.Item2)
                            {
                                logger.LogDebug("Submit {0} challenge for {1}.", challenge.Type, authz.Item1.Data.Identifier.Value);
                                await client.CompleteChallenge(challenge);
                            }
                        }

                        var authzFailed = false;
                        var pendingAuthz = authzChallenges.Select(a => a.Item1.Location).ToList();
                        while (pendingAuthz.Count > 0 && !authzFailed)
                        {
                            await Task.Delay(5000); // TODO: make configurable
                            var links = new List<Uri>(pendingAuthz);
                            pendingAuthz.Clear();
                            foreach (var link in links)
                            {
                                var authz = await client.GetAuthorization(link);
                                await this.contextStore.SetAuthorization(authz);
                                
                                if (authz.Data.Status == EntityStatus.Pending || authz.Data.Status == EntityStatus.Processing)
                                {
                                    pendingAuthz.Add(link);
                                }
                                else if (authz.Data.Status != EntityStatus.Valid)
                                {
                                    logger.LogWarning("Authorization failed for {0}.", authz.Data.Identifier.Value);
                                    authzFailed = true;
                                }
                            }
                        }
                        
                        foreach (var authz in authzChallenges)
                        {
                            foreach (var challenge in authz.Item2)
                            {
                                logger.LogDebug("Remove {0} responder for {1}.", challenge.Type, authz.Item1.Data.Identifier.Value);

                                var responder = await this.challengeResponderFactory.GetResponder(challenge.Type);
                                await responder.Remove(challenge);
                            }
                        }

                        if (authzFailed)
                        {
                            logger.LogWarning("Fail to renew certificate.");
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

                    logger.LogDebug("Submiting CSR - CN={0}, with {1} SAN.", hostNames.First(), hostNames.Count() - 1);
                    var cert = await client.NewCertificate(csr);

                    // TODO: check if the cert contains all the requested host names

                    var thumbprint = cert.GetThumbprint();
                    logger.LogDebug("Certificate generated {0}.", thumbprint);
                    var password = Guid.NewGuid().ToString("N"); // TODO: make configurable
                    var pfx = cert.ToPfx().Build($"certes-{thumbprint}", password);

                    logger.LogDebug("Installing certificate {0}.", thumbprint);
                    await bindingManager.InstallCertificate(thumbprint, pfx, password);
                    await bindingManager.UpdateSslBindings(thumbprint, hostNames);

                    logger.LogDebug("Certificate renewed.", thumbprint);
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
                    logger.LogDebug("Authz found for {0}.", id.Value);
                }
                else
                {
                    logger.LogDebug("Creating authz for {0}.", id.Value);
                    authz = await client.NewAuthorization(id);
                    await this.contextStore.SetAuthorization(authz);
                    
                    var challenges = await this.FindSupportedChallenges(authz);
                    if (challenges == null)
                    {
                        var challengesRequired = string.Join(", ", 
                            authz.Data.Combinations.Select(
                                combination => "[" + string.Join(", ", 
                                    combination.Select(i => "[" + authz.Data.Challenges[i].Type + "]")) +
                                    "]"));

                        logger.LogWarning("Can not complete authz for {0}. Require {1}", id.Value, challengesRequired);
                    }

                    authzChallenges.Add(Tuple.Create(authz, challenges));
                }
            }

            return authzChallenges;
        }

        private async Task<Challenge[]> FindSupportedChallenges(AcmeResult<Authorization> authz)
        {
            foreach (var combination in authz.Data.Combinations)
            {
                var hasResponder = true;
                foreach (var idx in combination)
                {
                    var challenge = authz.Data.Challenges[idx];
                    var responder = await this.challengeResponderFactory.GetResponder(challenge.Type);
                    if (responder == null)
                    {
                        hasResponder = false;
                        break;
                    }
                }

                if (hasResponder)
                {
                    return combination.Select(i => authz.Data.Challenges[i]).ToArray();
                }
            }

            return null;
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
            var renewDate = DateTimeOffset.Now.AddDays(options.RenewBeforeDays);
            bindingGroup = bindingGroup
                .Where(g => g.Any(b => b.CertificateThumbprint == null || b.CertificateExpires <= renewDate));

            return bindingGroup.Select(b => b.ToArray()).ToArray();
        }
    }
}
