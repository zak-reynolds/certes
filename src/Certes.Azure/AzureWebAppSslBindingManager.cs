using Microsoft.Azure.Management.WebSites;
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
        private TokenCredentials token;

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
                var siteResp = await client.Sites.GetSiteHostNameBindingsWithHttpMessagesAsync(
                    options.ResourceGroup, options.Name);
            }

            return null;
        }
    }
}
