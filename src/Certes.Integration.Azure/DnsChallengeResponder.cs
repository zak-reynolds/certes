using Certes.Acme;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Extensions.Options;
using Microsoft.Rest;
using System;
using System.Threading.Tasks;

namespace Certes.Integration.Azure
{
    public class DnsChallengeResponder : IChallengeResponder
    {
        private readonly IClientCredentialProvider accessTokenProvider;
        private readonly DnsOptions options;

        public DnsChallengeResponder(IClientCredentialProvider credentialProvider, IOptions<DnsOptions> options)
        {
            this.accessTokenProvider = credentialProvider;
            this.options = options.Value;
        }

        public string ChallengeType
        {
            get
            {
                return ChallengeTypes.Dns01;
            }
        }

        public Task Deploy(Challenge challenge)
        {
            throw new NotImplementedException();
        }

        public Task Remove(Challenge challenge)
        {
            throw new NotImplementedException();
        }

        private async Task<DnsManagementClient> CreateClient()
        {
            var token = await this.accessTokenProvider.GetOrCreateAccessToken();
            var credentials = new TokenCredentials(token);
            return new DnsManagementClient(credentials)
            {
                SubscriptionId = options.SubscriptionId
            };
        }
    }
}
