using Certes.Acme;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.Azure
{
    public interface IChallengeResponder
    {
        Task Deploy(Challenge challenge);
    }

    public interface IChallengeResponderFactory
    {
        Task<IChallengeResponder> GetResponder(string challengeType);
        bool IsSupported(string challengeType);
    }

    public class ChallengeResponderFactory : IChallengeResponderFactory
    {
        private readonly ConcurrentDictionary<string, IChallengeResponder> responders = new ConcurrentDictionary<string, IChallengeResponder>();

        public Task<IChallengeResponder> GetResponder(string challengeType)
        {
            IChallengeResponder responder;
            responders.TryGetValue(challengeType, out responder);
            return Task.FromResult(responder);
        }

        public bool IsSupported(string challengeType)
        {
            return responders.ContainsKey(challengeType);
        }
    }
}
