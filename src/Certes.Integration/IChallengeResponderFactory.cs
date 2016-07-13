using System.Threading.Tasks;

namespace Certes.Integration
{
    public interface IChallengeResponderFactory
    {
        Task<IChallengeResponder> GetResponder(string challengeType);
    }
}
