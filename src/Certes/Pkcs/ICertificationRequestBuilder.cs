using System.Collections.Generic;

namespace Certes.Pkcs
{
    /// <summary>
    /// Supports building Certificate Signing Request (CSR).
    /// </summary>
    public interface ICertificationRequestBuilder
    {
        /// <summary>
        /// Gets the subject alternative names.
        /// </summary>
        /// <value>
        /// The subject alternative names.
        /// </value>
        IList<string> SubjectAlternativeNames { get; }

        /// <summary>
        /// Adds the name.
        /// </summary>
        /// <param name="keyOrCommonName">Name of the key or common.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentException"></exception>
        void AddName(string keyOrCommonName, string value);

        /// <summary>
        /// Generates the CSR.
        /// </summary>
        /// <returns>The CSR data.</returns>
        byte[] Generate();

        /// <summary>
        /// Exports the key used to generate the CSR.
        /// </summary>
        /// <returns>The key data.</returns>
        KeyInfo Export();
    }
}
