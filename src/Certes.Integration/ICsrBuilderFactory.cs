using Certes.Pkcs;

namespace Certes.Integration
{
    public interface ICsrBuilderFactory
    {
        ICertificationRequestBuilder Create();
    }
}
