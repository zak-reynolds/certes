using Certes.Acme;
using System.Collections.Generic;

namespace Certes.Integration
{

    public class CertesContext
    {
        public AcmeAccount Account { get; set; }

        public Dictionary<AuthorizationIdentifier, AcmeResult<Authorization>> Authorizations { get; } =
            new Dictionary<AuthorizationIdentifier, AcmeResult<Authorization>>();
        public Dictionary<string, AcmeCertificate> Certificates { get; } = new Dictionary<string, AcmeCertificate>();
        public Dictionary<string, string> Bindings { get; } = new Dictionary<string, string>();
    }
}
