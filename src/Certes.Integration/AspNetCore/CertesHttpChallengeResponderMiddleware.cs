using Certes.Acme;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Certes.Integration
{
    public class CertesHttpChallengeResponderMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger logger;
        private readonly IHttpChallengeResponder challengeResponder;

        public CertesHttpChallengeResponderMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            IHttpChallengeResponder challengeResponder)
        {
            this.next = next;
            this.logger = loggerFactory.CreateLogger<CertesMiddleware>();
            this.challengeResponder = challengeResponder;
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.ToUriComponent();
            if (path?.Length > 1 && path.StartsWith("/"))
            {
                var host = context.Request.Host.Host;
                var token = path.Substring(1);

                var authzId = new AuthorizationIdentifier
                {
                    Type = AuthorizationIdentifierTypes.Dns,
                    Value = host
                };

                var keyAuthz = await this.challengeResponder.GetKeyAuthorizationString(token);

                if (keyAuthz != null)
                {
                    context.Response.ContentType = "plain/text";
                    await context.Response.WriteAsync(keyAuthz);
                    return;
                }
            }

            await next.Invoke(context);
        }
    }
}
