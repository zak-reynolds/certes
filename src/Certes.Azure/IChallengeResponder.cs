using Certes.Acme;
using System;
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
        IChallengeResponder GetResponder(string challengeType);
        bool IsSupported(string challengeType);
    }
}
