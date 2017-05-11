using Certes.Acme;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Certes.Integration
{
    public class InMemoryHttpChallengeResponder : IChallengeResponder, IHttpChallengeResponder
    {
        private static readonly Task CompletedTask = Task.FromResult(0);
        private readonly ConcurrentDictionary<string, Challenge> challenges = new ConcurrentDictionary<string, Challenge>();

        public string ChallengeType
        {
            get
            {
                return ChallengeTypes.Http01;
            }
        }

        public Task Deploy(Challenge challenge)
        {
            this.challenges.AddOrUpdate(challenge.Token, challenge, (token, existing) => challenge);
            return CompletedTask;
        }

        public Task<string> GetKeyAuthorizationString(string token)
        {
            Challenge challenge;
            this.challenges.TryGetValue(token, out challenge);
            return Task.FromResult(challenge.KeyAuthorization);
        }

        public Task Remove(Challenge challenge)
        {
            this.challenges.TryRemove(challenge.Token, out challenge);
            return CompletedTask;
        }
    }
}
