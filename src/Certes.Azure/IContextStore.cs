using Certes.Acme;
using System.Threading.Tasks;

namespace Certes.Azure
{
    public interface IContextStore
    {
        Task<AcmeAccount> GetAccount();
        Task SetAccount(AcmeAccount account);
        Task<AcmeResult<Authorization>> GetAuthorization(AuthorizationIdentifier identifier);
        Task SetAuthorization(AcmeResult<Authorization> authorization);
    }
}
