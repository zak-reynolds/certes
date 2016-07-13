using Certes.Acme;
using System;
using System.Collections.Generic;

namespace Certes.Integration
{
    public class CertesOptions
    {
        public Uri DirectoryUri { get; set; } = WellKnownServers.LetsEncrypt;
        public int MaxSanPerCert { get; set; } = 100;
        public int RenewBeforeDays { get; set; } = 30;
    }
}
