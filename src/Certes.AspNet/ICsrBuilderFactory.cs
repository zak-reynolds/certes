using Certes.Pkcs;

namespace Certes.AspNet
{
    public interface ICsrBuilderFactory
    {
        ICertificationRequestBuilder Create();
    }
}
