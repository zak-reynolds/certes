using Certes.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Certes
{
    public static class CertesOptionsBuilderExtensions
    {
        public static CertesOptionsBuilder AddInMemoryProviders(this CertesOptionsBuilder builder)
        {
            var contextStore = new InMemoryContextStore();
            var httpResponder = new InMemoryHttpChallengeResponder();

            builder.Services.AddSingleton<IContextStore>(contextStore);
            builder.Services.AddSingleton<IChallengeResponder>(httpResponder);
            builder.Services.AddSingleton<IHttpChallengeResponder>(httpResponder);
            return builder;
        }

        public static CertesOptionsBuilder UseConfiguration(this CertesOptionsBuilder builder, IConfiguration config)
        {
            var certesSection = config.GetSection("certes");
            builder.Services.Configure<CertesOptions>(certesSection);
            
            return builder;
        }
    }
}
