using System.Threading.Tasks;

namespace Certes.AspNet
{
    public interface IHttpChallengeResponder
    {
        Task<string> GetKeyAuthorizationString(string token);
    }
}
