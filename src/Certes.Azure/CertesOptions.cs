using Certes.Acme;
using System;

namespace Certes.Azure
{
    public class CertesOptions
    {
        public Uri DirectoryUri { get; set; } = WellKnownServers.LetsEncrypt;
        public int MaxSanPerCert { get; set; } = 100;
        public TimeSpan RenewBeforeExpire { get; set; } = TimeSpan.FromDays(30);
    }
}
