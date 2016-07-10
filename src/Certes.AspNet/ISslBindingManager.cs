using System.Collections.Generic;
using System.Threading.Tasks;

namespace Certes.AspNet
{
    public interface ISslBindingManager
    {
        Task<IList<SslBinding>> GetHostNames();
        Task InstallCertificate(string certificateThumbprint, byte[] pfxBlob, string password);
        Task UpdateSslBindings(string certificateThumbprint, IList<string> hostNames);
    }
}
