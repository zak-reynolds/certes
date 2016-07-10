using Microsoft.Azure.Management.WebSites;
using Microsoft.Azure.Management.WebSites.Models;
using Microsoft.Extensions.Options;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.AspNet.Azure
{
    public class AzureWebAppSslBindingManager : ISslBindingManager
    {
        private readonly IAzureClientCredentialProvider accessTokenProvider;
        private readonly AzureWebAppOptions options;

        public AzureWebAppSslBindingManager(IOptions<AzureWebAppOptions> options, IAzureClientCredentialProvider accessTokenProvider)
        {
            this.options = options.Value;
            this.accessTokenProvider = accessTokenProvider;
        }

        public async Task<IList<SslBinding>> GetHostNames()
        {
            using (var client = await CreateClient())
            {
                var sites = await client.Sites.GetSiteAsync(options.ResourceGroup, options.Name);
                var bindings = sites.HostNameSslStates
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
                    var cert = await client.Certificates.GetCertificateAsync(options.ResourceGroup, thumbprint);
                    var exp = cert.ExpirationDate.HasValue ? cert.ExpirationDate.Value : DateTime.MaxValue;
                    foreach (var binding in group)
                    {
                        binding.CertificateExpires = exp;
                    }
                }

                return bindings;
            }
        }

        public async Task InstallCertificate(string certificateThumbprint, byte[] pfxBlob, string password)
        {
            using (var client = await CreateClient())
            {
                await client.Certificates.CreateOrUpdateCertificateAsync(options.ResourceGroup, certificateThumbprint, new Certificate
                {
                    PfxBlob = Convert.ToBase64String(pfxBlob),
                    Password = password
                });
            }
        }

        public async Task UpdateSslBindings(string certificateThumbprint, IList<string> hostNames)
        {
            using (var client = await CreateClient())
            {
                await client.Certificates.CreateOrUpdateCertificateAsync(options.ResourceGroup, certificateThumbprint, new Certificate
                {
                    HostNames = hostNames
                });
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
