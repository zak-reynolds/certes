using Certes.Acme;
using System;
using System.Threading.Tasks;

namespace Certes.AspNet.Azure
{
    public class DnsChallengeResponder : IChallengeResponder
    {
        private readonly IClientCredentialProvider credentialProvider;

        public DnsChallengeResponder(IClientCredentialProvider credentialProvider)
        {
            this.credentialProvider = credentialProvider;
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
    }
}
