using Certes.Pkcs;

namespace Certes.Azure
{
    public interface ICsrBuilderFactory
    {
        ICertificationRequestBuilder Create();
    }
}
