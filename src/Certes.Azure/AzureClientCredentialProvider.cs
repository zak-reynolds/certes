using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.Azure
{

    public class AzureClientCredentialProvider : IAzureClientCredentialProvider
    {
        private readonly AzureManagementClientOptions options;
        private AuthenticationResult token;

        public AzureClientCredentialProvider(IOptions<AzureManagementClientOptions> options)
        {
            this.options = options.Value;
        }

        public async Task<string> GetOrCreateAccessToken()
        {
            if (token == null || token.ExpiresOn < DateTimeOffset.Now)
            {
                var authContext = new AuthenticationContext($"https://login.windows.net/{options.TenantId}");
                var credential = new ClientCredential(options.ClientId, options.ClientSecret);
                this.token = await authContext.AcquireTokenAsync("https://management.azure.com/", credential);
            }

            return token.AccessToken;
        }
    }

    public interface IAzureClientCredentialProvider
    {
        Task<string> GetOrCreateAccessToken();
    }
}
