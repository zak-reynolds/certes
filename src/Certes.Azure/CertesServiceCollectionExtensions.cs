using Microsoft.Extensions.DependencyInjection;
using System;

namespace Certes.Azure
{
    public static class CertesServiceCollectionExtensions
    {
        public static CertesBuilder AddCertes(this IServiceCollection services)
        {
            return services.AddCertes(setupAction: null);
        }

        public static CertesBuilder AddCertes(this IServiceCollection services, Action<CertesOptions> setupAction)
        {
            services.AddScoped<IChallengeResponderFactory, ChallengeResponderFactory>();

            if (setupAction != null)
            {
                services.Configure(setupAction);
            }

            return new CertesBuilder(services);
        }
    }
}
