using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Certes.Azure
{
    public class CertesMiddleware
    {
        private const string WebJobFilePrefix = "Certes.Azure.Resources.WebJob.";
        private bool webJobInitialized = false;

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
            if (!webJobInitialized)
            {
                var env = context.RequestServices.GetRequiredService<IHostingEnvironment>();
                var webJobPath = Path.Combine(env.ContentRootPath, "App_Data/Triggered/Certes");

                var assembly = typeof(CertesMiddleware).GetTypeInfo().Assembly;
                var webJobFiles = assembly
                    .GetManifestResourceNames()
                    .Where(n => n.StartsWith(WebJobFilePrefix))
                    .ToArray();

                foreach (var file in webJobFiles)
                {
                    var filename = file.Substring(WebJobFilePrefix.Length);
                    var dest = Path.Combine(webJobPath, filename);
                    using (var destStream = File.Create(dest))
                    using (var srcStream = assembly.GetManifestResourceStream(dest))
                    {
                        srcStream.CopyTo(destStream);
                    }
                }
            }

            await next.Invoke(context);
        }
    }

    public static class CertesExtensions
    {

        public static IApplicationBuilder UseCertesWebJob(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CertesMiddleware>();
        }
    }
}
