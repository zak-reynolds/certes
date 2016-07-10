using Certes.AspNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Certes
{
    public static class CertesApplicationBuilderExtensions
    {
        private const string WebJobFilePrefix = "Certes.Azure.Resources.WebJob.";
        private static Task webJobDeploymentTask;

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

        public static IApplicationBuilder UseCertesWebJobScheduler(this IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            var webJobPath = Path.Combine(env.ContentRootPath, "app_data/jobs/triggered/certes");

            webJobDeploymentTask = DeployWebJobScheduler(webJobPath)
                .ContinueWith(tsk =>
                {
                    webJobDeploymentTask = null;
                });

            return app;
        }

        private static async Task DeployWebJobScheduler(string webJobPath)
        {
            var dir = new DirectoryInfo(webJobPath);
            if (!dir.Exists)
            {
                dir.Create();
            }

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
                using (var srcStream = assembly.GetManifestResourceStream(file))
                {
                    await srcStream.CopyToAsync(destStream);
                }
            }
        }
    }
}
