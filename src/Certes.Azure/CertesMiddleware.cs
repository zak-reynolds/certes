using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Certes.Azure
{
    public class CertesMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger logger;
        private readonly CertesOptions options;

        public CertesMiddleware(RequestDelegate next, IOptions<CertesOptions> optionsAccessor, LoggerFactory loggerFactory)
        {
            this.next = next;
            this.logger = loggerFactory.CreateLogger<CertesMiddleware>();
            this.options = optionsAccessor.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            await next.Invoke(context);
        }
    }

    public static class CertesExtensions
    {
        public static IApplicationBuilder UseRequestLogger(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CertesMiddleware>();
        }
    }
}
