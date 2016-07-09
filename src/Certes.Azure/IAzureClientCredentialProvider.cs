using System.Threading.Tasks;

namespace Certes.Azure
{
    public interface IAzureClientCredentialProvider
    {
        Task<string> GetOrCreateAccessToken();
    }
}
