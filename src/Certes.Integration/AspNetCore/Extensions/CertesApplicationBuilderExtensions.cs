using Certes.Integration;
using Microsoft.AspNetCore.Builder;

namespace Certes
{
    public static class CertesApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCertes(this IApplicationBuilder app)
        {
            app.Map("/.certes/renew", sub =>
            {
                sub.UseMiddleware<CertesMiddleware>();
            });

            return app;
        }

        public static IApplicationBuilder UseCertesHttpChallengeResponder(this IApplicationBuilder app)
        {
            app.Map("/.well-known/acme-challenge", sub =>
            {
                sub.UseMiddleware<CertesHttpChallengeResponderMiddleware>();
            });

            return app;
        }
    }
}
