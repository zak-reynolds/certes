using System.Collections.Generic;
using System.Threading.Tasks;

namespace Certes.Integration
{
    /// <summary>
    /// Supports updating SSL bindings.
    /// </summary>
    public interface ISslBindingManager
    {
        /// <summary>
        /// Gets the host names with SSL binding state.
        /// </summary>
        /// <returns>The host names.</returns>
        Task<IList<SslBinding>> GetHostNames();

        /// <summary>
        /// Installs the certificate.
        /// </summary>
        /// <param name="certificateThumbprint">The certificate thumbprint.</param>
        /// <param name="pfxBlob">The PFX BLOB.</param>
        /// <param name="password">The password.</param>
        /// <returns>The awaitable.</returns>
        Task InstallCertificate(string certificateThumbprint, byte[] pfxBlob, string password);

        /// <summary>
        /// Updates the SSL bindings.
        /// </summary>
        /// <param name="certificateThumbprint">The certificate thumbprint for the SSL bindings.</param>
        /// <param name="hostNames">The host names.</param>
        /// <returns>The awaitable.</returns>
        Task UpdateSslBindings(string certificateThumbprint, params string[] hostNames);
    }
}
