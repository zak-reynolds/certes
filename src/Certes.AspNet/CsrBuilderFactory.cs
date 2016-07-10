using Certes.Pkcs;

namespace Certes.AspNet
{

    public class CsrBuilderFactory : ICsrBuilderFactory
    {
        public ICertificationRequestBuilder Create()
        {
            return new CertificationRequestBuilder();
        }
    }
}
