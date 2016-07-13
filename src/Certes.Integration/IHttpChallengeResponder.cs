using System.Threading.Tasks;

namespace Certes.Integration
{
    public interface IHttpChallengeResponder
    {
        Task<string> GetKeyAuthorizationString(string token);
    }
}
