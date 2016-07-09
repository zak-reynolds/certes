using System.Threading.Tasks;

namespace Certes.Azure
{
    public interface IChallengeResponderFactory
    {
        Task<IChallengeResponder> GetResponder(string challengeType);
    }
}
