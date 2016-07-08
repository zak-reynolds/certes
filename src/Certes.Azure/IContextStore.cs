using Certes.Acme;
using System.Collections.Concurrent;
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

    public class InMemoryContextStore : IContextStore
    {
        private AcmeAccount account;
        private readonly ConcurrentDictionary<AuthorizationIdentifier, AcmeResult<Authorization>> authorizations = new ConcurrentDictionary<AuthorizationIdentifier, AcmeResult<Authorization>>();

        public Task<AcmeAccount> GetAccount()
        {
            return Task.FromResult(account);
        }

        public Task<AcmeResult<Authorization>> GetAuthorization(AuthorizationIdentifier identifier)
        {
            AcmeResult<Authorization> authz;
            authorizations.TryGetValue(identifier, out authz);
            return Task.FromResult(authz);
        }

        public Task SetAccount(AcmeAccount account)
        {
            this.account = account;
            return Task.CompletedTask;
        }

        public Task SetAuthorization(AcmeResult<Authorization> authorization)
        {
            this.authorizations.AddOrUpdate(authorization.Data.Identifier, authorization, (id, authz) => authorization);
            return Task.CompletedTask;
        }
    }
}
