using Certes.Integration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Certes
{
    public static class CertesServiceCollectionExtensions
    {
        public static IServiceCollection AddCertes(this IServiceCollection services, Action<CertesOptionsBuilder> setupAction)
        {
            services.AddScoped<IChallengeResponderFactory, ChallengeResponderFactory>();
            services.AddScoped<ICsrBuilderFactory, CsrBuilderFactory>();

            var builder = new CertesOptionsBuilder(services);

            setupAction?.Invoke(builder);
            
            return services;
        }
    }
}
