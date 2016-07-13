using System.Threading.Tasks;

namespace Certes.Integration
{
    public class InMemoryContextStore : IContextStore
    {
        private CertesContext context;

        public Task<CertesContext> Load(bool exclusive)
        {
            return Task.FromResult(context ?? (context = new CertesContext()));
        }

        public Task Save(CertesContext context, bool release)
        {
            this.context = context;
            return Task.CompletedTask;
        }
    }
}
