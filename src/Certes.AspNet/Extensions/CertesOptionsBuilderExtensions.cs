using Certes.AspNet;
using Certes.AspNet.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace Certes
{
    public static class CertesOptionsBuilderExtensions
    {
        public static CertesOptionsBuilder UseServicePrincipal(
            this CertesOptionsBuilder builder, 
            string tenantId,
            string clientId,
            string clientSecret)
        {
            builder.Services.Configure<ServicePrincipalOptions>(options =>
            {
                options.TenantId = tenantId;
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
            });

            builder.Services.AddScoped<IClientCredentialProvider, ServicePrincipalCredentialProvider>();

            return builder;
        }

        public static CertesOptionsBuilder ForWebApp(
            this CertesOptionsBuilder builder,
            string subscriptionId,
            string resourceGroup,
            string name)
        {
            builder.Services.Configure<WebAppOptions>(options =>
            {
                options.SubscriptionId = subscriptionId;
                options.ResourceGroup = resourceGroup;
                options.Name = name;
            });

            builder.Services.AddScoped<ISslBindingManager, WebAppSslBindingManager>();

            return builder;
        }

        public static CertesOptionsBuilder AddHttpChallengeResponder(this CertesOptionsBuilder builder)
        {
            return builder;
        }
    }
}
