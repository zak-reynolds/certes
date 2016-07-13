using System.Threading.Tasks;

namespace Certes.Integration
{
    public interface IContextStore
    {
        Task<CertesContext> Load(bool exclusive = false);
        Task Save(CertesContext context, bool release = false);
    }
}
