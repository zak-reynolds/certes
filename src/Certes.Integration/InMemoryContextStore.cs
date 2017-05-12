using Certes.Acme;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Certes.Integration
{
    public class InMemoryContextStore : IContextStore
    {
        private static readonly Task CompletedTask = Task.FromResult(0);
        private AcmeAccount account;
        private readonly Dictionary<AuthorizationIdentifier, AcmeResult<Authorization>> authorizations = new Dictionary<AuthorizationIdentifier, AcmeResult<Authorization>>();

        public ValueTask<AcmeResult<Authorization>> Get(AuthorizationIdentifier identifier)
        {
            authorizations.TryGetValue(identifier, out var authz);
            return new ValueTask<AcmeResult<Authorization>>(authz);
        }

        public async ValueTask<AcmeAccount> GetOrCreate(Func<ValueTask<AcmeAccount>> provider)
        {
            return account ?? (account = await provider.Invoke());
        }

        public Task Save(AcmeResult<Authorization> authorization)
        {
            authorizations[authorization.Data.Identifier] = authorization;
            return CompletedTask;
        }
    }
}
