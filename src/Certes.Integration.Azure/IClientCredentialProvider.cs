using System.Threading.Tasks;

namespace Certes.Integration.Azure
{
    public interface IClientCredentialProvider
    {
        Task<string> GetOrCreateAccessToken();
    }
}
