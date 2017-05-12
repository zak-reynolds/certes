using Certes.Acme;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.Integration
{
    /// <summary>
    /// Middleware supports certificate renewal.
    /// </summary>
    public class CertesMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger logger;
        private readonly CertesOptions options;
        private readonly ISslBindingManager bindingManager;
        private readonly IContextStore contextStore;
        private readonly IChallengeResponderFactory challengeResponderFactory;
        private readonly ICsrBuilderFactory csrBuilderFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertesMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware.</param>
        /// <param name="optionsAccessor">The options accessor.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="contextStore">The context store.</param>
        /// <param name="challengeResponderFactory">The challenge responder factory.</param>
        /// <param name="csrBuilderFactory">The CSR builder factory.</param>
        /// <param name="bindingManager">The SSL binding manager.</param>
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

        /// <summary>
        /// Processs the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The awaitable.</returns>
        public async Task Invoke(HttpContext context)
        {
            logger.LogDebug("Start renewing SSL certificates.");

            var bindings = await GetSslBindingsForRenewal();
            if (bindings.Length == 0)
            {
                // All host names have valid SSL
                logger.LogDebug("All host names have valid SSL bindings.");

                return;
            }

            using (var client = new AcmeClient(options.DirectoryUri))
            {
                logger.LogDebug("Using ACME server {0}.", options.DirectoryUri);

                var account = await contextStore.GetOrCreate(async () =>
                {
                    logger.LogDebug("No ACME account configured, register new account.");
                    var reg = await client.NewRegistraton(); // TODO: add optional contact method
                    reg.Data.Agreement = reg.GetTermsOfServiceUri();
                    return await client.UpdateRegistration(reg);
                });

                client.Use(account.Key);
                
                for (int i = 0; i < bindings.Length; i += options.MaxSanPerCert)
                {
                    await RenewCertificates(bindings.Skip(i).Take(options.MaxSanPerCert), client);
                }
            }

            await next.Invoke(context);
        }

        private async Task<bool> RenewCertificates(IEnumerable<SslBinding> bindings, AcmeClient client)
        {
            logger.LogDebug("Start renewing certificate.");
            var challenges = await GetChallenges(client, bindings);

            if (challenges.Any(c => c.supportedChallenges == null))
            {
                logger.LogDebug("Discard pending authorizations.");
                // There's at least one authz we can not complete, discard all authz in current group
                foreach (var authz in challenges.Select(c => c.Item1))
                {
                    await client.CompleteChallenge(authz.Data.Challenges.First());
                }

                // TODO: log warning
                return false;
            }
            else
            {
                foreach (var authz in challenges)
                {
                    foreach (var challenge in authz.supportedChallenges)
                    {
                        // Make sure the key authz string is ready
                        challenge.KeyAuthorization = client.ComputeKeyAuthorization(challenge);

                        logger.LogDebug("Deploy {0} responder for {1}.", challenge.Type, authz.authz.Data.Identifier.Value);
                        var responder = await this.challengeResponderFactory.GetResponder(challenge.Type);
                        await responder.Deploy(challenge);
                    }
                }

                // TODO: Maybe an optional delay to avoid caching issues?

                foreach (var authz in challenges)
                {
                    foreach (var challenge in authz.supportedChallenges)
                    {
                        logger.LogDebug("Submit {0} challenge for {1}.", challenge.Type, authz.authz.Data.Identifier.Value);
                        await client.CompleteChallenge(challenge);
                    }
                }

                var authzFailed = false;
                var pendingAuthz = challenges.Select(a => a.Item1.Location).ToList();
                while (pendingAuthz.Count > 0 && !authzFailed)
                {
                    await Task.Delay(5000); // TODO: make configurable
                    var links = new List<Uri>(pendingAuthz);
                    pendingAuthz.Clear();
                    foreach (var link in links)
                    {
                        var authz = await client.GetAuthorization(link);
                        await contextStore.Save(authz);

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

                foreach (var authz in challenges)
                {
                    foreach (var challenge in authz.supportedChallenges)
                    {
                        logger.LogDebug("Remove {0} responder for {1}.", challenge.Type, authz.authz.Data.Identifier.Value);

                        var responder = await this.challengeResponderFactory.GetResponder(challenge.Type);
                        await responder.Remove(challenge);
                    }
                }

                if (authzFailed)
                {
                    logger.LogWarning("Fail to renew certificate.");
                    return false;
                }
            }

            var hostNames = bindings.Select(g => g.HostName).ToArray();

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
            return true;
        }

        private async ValueTask<List<(AcmeResult<Authorization> authz, Challenge[] supportedChallenges)>> GetChallenges(
            AcmeClient client, IEnumerable<SslBinding> bindingGroup)
        {
            var authzChallenges = new List<(AcmeResult<Authorization>, Challenge[])>();

            foreach (var binding in bindingGroup)
            {
                var id = new AuthorizationIdentifier
                {
                    Type = AuthorizationIdentifierTypes.Dns,
                    Value = binding.HostName
                };

                var authz = await contextStore.Get(id);

                if (authz?.Data?.Status == EntityStatus.Pending)
                {
                    authz = await client.GetAuthorization(authz.Location);
                }

                if (authz?.Data?.Status == EntityStatus.Valid && authz?.Data?.Expires < DateTimeOffset.Now.AddHours(-1))
                {
                    logger.LogDebug("Authz found for {0}.", id.Value);
                }
                else
                {
                    logger.LogDebug("Creating authz for {0}.", id.Value);
                    authz = await client.NewAuthorization(id);
                    await contextStore.Save(authz);

                    var challenges = await this.FindSupportedChallenges(authz);
                    if (challenges == null)
                    {
                        var challengesRequired = string.Join(", ",
                            authz.Data.Combinations.Select(
                                combination => "[" + string.Join(", ",
                                    combination.Select(i => $"[{authz.Data.Challenges[i].Type}]")) +
                                    "]"));

                        logger.LogWarning("Can not complete authz for {0}. Require {1}", id.Value, challengesRequired);
                    }

                    authzChallenges.Add((authz, challenges));
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

        private async ValueTask<SslBinding[]> GetSslBindingsForRenewal()
        {
            var renewDate = DateTimeOffset.Now.AddDays(options.RenewBeforeDays);
            var hostNames = await bindingManager.GetHostNames();
            return hostNames.Where(n => n.CertificateThumbprint == null || n.CertificateExpiryDate <= renewDate).ToArray();

        }
    }
}
