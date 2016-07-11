using System.Threading.Tasks;

namespace Certes.AspNet.Azure
{
    public interface IClientCredentialProvider
    {
        Task<string> GetOrCreateAccessToken();
    }
}
