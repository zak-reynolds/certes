using Certes.Pkcs;
using Org.BouncyCastle.Crypto.Digests;
using System;

namespace Certes.Acme
{
    /// <summary>
    /// Represents a ACME <see cref="Certificate"/>.
    /// </summary>
    public class AcmeCertificate : KeyedAcmeResult<string>
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="AcmeCertificate"/> is revoked.
        /// </summary>
        /// <value>
        ///   <c>true</c> if revoked; otherwise, <c>false</c>.
        /// </value>
        public bool Revoked { get; set; }

        /// <summary>
        /// Gets or sets the issuer certificate.
        /// </summary>
        /// <value>
        /// The issuer certificate.
        /// </value>
        public AcmeCertificate Issuer { get; set; }
    }

    /// <summary>
    /// Helper methods for <see cref="AcmeCertificate"/>.
    /// </summary>
    public static class AcmeCertificateExtensions
    {
        /// <summary>
        /// Converts the certificate To the PFX builder.
        /// </summary>
        /// <param name="cert">The certificate.</param>
        /// <returns>The PFX builder.</returns>
        /// <exception cref="System.Exception">If the certificate data is missing.</exception>
        public static PfxBuilder ToPfx(this AcmeCertificate cert)
        {
            if (cert?.Raw == null)
            {
                throw new Exception($"Certificate data missing, please fetch the certificate from ${cert.Location}");
            }

            var pfxBuilder = new PfxBuilder(cert.Raw, cert.Key);
            var issuer = cert.Issuer;
            while (issuer != null)
            {
                pfxBuilder.AddIssuer(issuer.Raw);
                issuer = issuer.Issuer;
            }

            return pfxBuilder;
        }

        /// <summary>
        /// Gets the thumbprint for <paramref name="cert"/>.
        /// </summary>
        /// <param name="cert">The cert.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">If the certificate data is missing.</exception>
        public static string GetThumbprint(this AcmeCertificate cert)
        {
            if (cert?.Raw == null)
            {
                throw new Exception($"Certificate data missing, please fetch the certificate from ${cert.Location}");
            }

            var data = cert.Raw;

            var sha1 = new Sha1Digest();
            var hashed = new byte[sha1.GetDigestSize()];

            sha1.BlockUpdate(data, 0, data.Length);
            sha1.DoFinal(hashed, 0);


            return BitConverter.ToString(hashed).Replace("-", "");
        }
    }
}
