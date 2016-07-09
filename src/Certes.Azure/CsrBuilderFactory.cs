using Certes.Pkcs;

namespace Certes.Azure
{

    public class CsrBuilderFactory : ICsrBuilderFactory
    {
        public ICertificationRequestBuilder Create()
        {
            return new CertificationRequestBuilder();
        }
    }
}
