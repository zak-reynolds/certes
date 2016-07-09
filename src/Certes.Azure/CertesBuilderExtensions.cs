using Microsoft.Extensions.DependencyInjection;

namespace Certes.Azure
{
    public static class CertesBuilderExtensions
    {
        public static CertesBuilder AddAzure(this CertesBuilder builder)
        {
            return builder;
        }
    }
}
