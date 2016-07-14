using System;

namespace Certes.Integration
{
    /// <summary>
    /// Represents a SSL binding.
    /// </summary>
    public class SslBinding
    {
        /// <summary>
        /// Gets or sets the name of the host.
        /// </summary>
        /// <value>
        /// The name of the host.
        /// </value>
        public string HostName { get; set; }

        /// <summary>
        /// Gets or sets the certificate thumbprint.
        /// </summary>
        /// <value>
        /// The certificate thumbprint.
        /// </value>
        public string CertificateThumbprint { get; set; }

        /// <summary>
        /// Gets or sets the certificate expiry date.
        /// </summary>
        /// <value>
        /// The certificate expiry date.
        /// </value>
        public DateTimeOffset CertificateExpiryDate { get; set; }
    }
}
