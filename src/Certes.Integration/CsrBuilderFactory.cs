using Certes.Pkcs;

namespace Certes.Integration
{

    public class CsrBuilderFactory : ICsrBuilderFactory
    {
        public ICertificationRequestBuilder Create()
        {
            return new CertificationRequestBuilder();
        }
    }
}
