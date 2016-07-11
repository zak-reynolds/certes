using Certes.Acme;
using System;
using System.Collections.Generic;

namespace Certes.AspNet
{
    public class CertesOptions
    {
        public Uri DirectoryUri { get; set; } = WellKnownServers.LetsEncrypt;
        public int MaxSanPerCert { get; set; } = 100;
        public TimeSpan RenewBeforeExpire { get; set; } = TimeSpan.FromDays(30);
    }
}
