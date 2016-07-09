using Certes.Acme;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.Azure
{
    public class CertesHttpChallengeResponderMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger logger;
        private readonly IContextStore contextStore;

        public CertesHttpChallengeResponderMiddleware(
            RequestDelegate next,
            IOptions<CertesOptions> optionsAccessor,
            ILoggerFactory loggerFactory,
            IContextStore contextStore,
            IChallengeResponderFactory challengeResponderFactory,
            ICsrBuilderFactory csrBuilderFactory,
            ISslBindingManager bindingManager)
        {
            this.next = next;
            this.logger = loggerFactory.CreateLogger<CertesMiddleware>();
            this.contextStore = contextStore;
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

                var authz = await this.contextStore.GetAuthorization(authzId);
                var keyAuthz = authz?.Data?.Challenges?
                    .Where(c => c.Type == ChallengeTypes.Http01 && c.Token == token)
                    .Select(c => c.KeyAuthorization)
                    .FirstOrDefault();

                if (keyAuthz != null)
                {
                    context.Response.ContentType = "plain/text";
                    await context.Response.WriteAsync(keyAuthz);
                }
            }

            await next.Invoke(context);
        }
    }
}
