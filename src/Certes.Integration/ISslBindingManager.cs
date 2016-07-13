using System.Collections.Generic;
using System.Threading.Tasks;

namespace Certes.Integration
{
    public interface ISslBindingManager
    {
        Task<IList<SslBinding>> GetHostNames();
        Task InstallCertificate(string certificateThumbprint, byte[] pfxBlob, string password);
        Task UpdateSslBindings(string certificateThumbprint, params string[] hostNames);
    }
}
