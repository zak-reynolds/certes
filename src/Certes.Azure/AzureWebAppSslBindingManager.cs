using Microsoft.Azure.Management.WebSites;
using Microsoft.Azure.Management.WebSites.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.Azure
{
    public class AzureWebAppSslBindingManager : ISslBindingManager
    {
        private readonly AzureWebAppManagementOptions options;

        public AzureWebAppSslBindingManager(IOptions<AzureWebAppManagementOptions> options)
        {
            this.options = options.Value;
        }

        public async Task<IList<SslBinding>> GetHostNames()
        {
            var authContext = new AuthenticationContext($"https://login.windows.net/{options.TenantId}");
            var credential = new ClientCredential(options.ClientId, options.ClientSecret);
            var token = await authContext.AcquireTokenAsync("https://management.azure.com/", credential);
            var credentials = new TokenCredentials(token.AccessToken);

            using (var client = new WebSiteManagementClient(credentials)
            {
                SubscriptionId = options.SubscriptionId
            })
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
    }
}
