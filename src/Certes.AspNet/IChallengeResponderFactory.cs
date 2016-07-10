using System.Threading.Tasks;

namespace Certes.AspNet
{
    public interface IChallengeResponderFactory
    {
        Task<IChallengeResponder> GetResponder(string challengeType);
    }
}
