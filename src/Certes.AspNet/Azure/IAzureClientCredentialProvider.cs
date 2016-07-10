using System.Threading.Tasks;

namespace Certes.AspNet.Azure
{
    public interface IAzureClientCredentialProvider
    {
        Task<string> GetOrCreateAccessToken();
    }
}
