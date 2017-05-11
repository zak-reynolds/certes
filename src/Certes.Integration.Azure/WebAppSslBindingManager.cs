using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Extensions.Options;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.Integration.Azure
{
    public class WebAppSslBindingManager : ISslBindingManager
    {
        private readonly IClientCredentialProvider accessTokenProvider;
        private readonly WebAppOptions options;

        public WebAppSslBindingManager(IOptions<WebAppOptions> options, IClientCredentialProvider accessTokenProvider)
        {
            this.options = options.Value;
            this.accessTokenProvider = accessTokenProvider;
        }

        public async Task<IList<SslBinding>> GetHostNames()
        {
            using (var client = await CreateClient())
            {
                var site = await client.WebApps.GetAsync(options.ResourceGroup, options.Name);
                var bindings = site.HostNameSslStates
                    .Where(h => !h.Name.EndsWith(".azurewebsites.net") && !h.Name.EndsWith(".trafficmanager.net"))
                    .Select(h => new SslBinding
                    {
                        HostName = h.Name,
                        CertificateThumbprint = h.SslState == SslState.Disabled ? null : h.Thumbprint
                    })
                    .ToArray();

                foreach (var group in bindings.Where(b => b.CertificateThumbprint != null).GroupBy(b => b.CertificateThumbprint))
                {
                    var thumbprint = group.Select(g => g.CertificateThumbprint).First();
                    var cert = await client.Certificates.GetAsync(options.ResourceGroup, thumbprint);
                    var exp = cert.ExpirationDate.HasValue ? cert.ExpirationDate.Value : DateTime.MaxValue;
                    foreach (var binding in group)
                    {
                        binding.CertificateExpiryDate = exp;
                    }
                }

                return bindings;
            }
        }

        public async Task InstallCertificate(string certificateThumbprint, byte[] pfxBlob, string password)
        {
            using (var client = await CreateClient())
            {
                var site = await client.WebApps.GetAsync(options.ResourceGroup, options.Name);
                await client.Certificates.CreateOrUpdateAsync(options.ResourceGroup, certificateThumbprint, new CertificateInner
                {
                    Location = site.Location,
                    PfxBlob = pfxBlob,
                    Password = password
                });
            }
        }

        public async Task UpdateSslBindings(string certificateThumbprint, params string[] hostNames)
        {
            using (var client = await CreateClient())
            {
                var site = await client.WebApps.GetAsync(options.ResourceGroup, options.Name);

                
                foreach (var bindingState in site.HostNameSslStates.Where(h => hostNames.Contains(h.Name)))
                {
                    bindingState.Thumbprint = certificateThumbprint;
                    bindingState.SslState = SslState.SniEnabled;
                    bindingState.ToUpdate = true;
                }
                
                await client.WebApps.CreateOrUpdateAsync(
                    options.ResourceGroup, options.Name, site);
            }
        }

        private async Task<WebSiteManagementClient> CreateClient()
        {
            var token = await this.accessTokenProvider.GetOrCreateAccessToken();
            var credentials = new TokenCredentials(token);
            return new WebSiteManagementClient(credentials)
            {
                SubscriptionId = options.SubscriptionId
            };
        }
    }

}
