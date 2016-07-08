using System;
using Certes.Pkcs;

namespace Certes.Azure
{
    public interface ICsrBuilderFactory
    {
        ICertificationRequestBuilder Create();
    }

    public class CsrBuilderFactory : ICsrBuilderFactory
    {
        public ICertificationRequestBuilder Create()
        {
            return new CertificationRequestBuilder();
        }
    }
}
