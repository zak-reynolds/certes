using Certes.Acme;
using System;

namespace Certes.Azure
{
    public class CertesOptions
    {
        public Uri DirectoryUri { get; set; } = WellKnownServers.LetsEncrypt;
    }
}
