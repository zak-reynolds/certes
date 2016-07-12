using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.AspNet
{
    public class ChallengeResponderFactory : IChallengeResponderFactory
    {
        private readonly IChallengeResponder[] responders;

        public ChallengeResponderFactory(IEnumerable<IChallengeResponder> responders)
        {
            this.responders = responders.ToArray();
        }

        public Task<IChallengeResponder> GetResponder(string challengeType)
        {
            var responder = this.responders.Where(r => r.ChallengeType == challengeType).FirstOrDefault();
            return Task.FromResult(responder);
        }
    }
}
