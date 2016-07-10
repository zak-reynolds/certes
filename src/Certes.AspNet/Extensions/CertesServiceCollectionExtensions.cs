using Certes.AspNet;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Certes
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
            services.AddScoped<ICsrBuilderFactory, CsrBuilderFactory>();
            services.AddScoped<IContextStore, InMemoryContextStore>();

            if (setupAction != null)
            {
                services.Configure(setupAction);
            }

            return new CertesBuilder(services);
        }
    }
}
