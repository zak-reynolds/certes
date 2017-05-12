using Certes.Acme;
using System;
using System.Threading.Tasks;

namespace Certes.Integration
{
    public interface IContextStore
    {
        ValueTask<AcmeAccount> GetOrCreate(Func<ValueTask<AcmeAccount>> provider);

        ValueTask<AcmeResult<Authorization>> Get(AuthorizationIdentifier identifier);
        Task Save(AcmeResult<Authorization> authorization);
    }
}
