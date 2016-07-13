using Certes.Integration.Azure;
using Certes.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Certes
{
    public static class CertesAzureApplicationBuilderExtensions
    {
        private const string WebJobFilePrefix = "Resources.WebJob.";
        private static Task webJobDeploymentTask;
        
        public static IApplicationBuilder UseCertesWebJobScheduler(this IApplicationBuilder app)
        {
            var serviceProvider = app.ApplicationServices;
            var env = serviceProvider.GetRequiredService<IHostingEnvironment>();
            var options = serviceProvider.GetService<IOptions<WebJobSchedulerOptions>>();
            
            var webJobPath = Path.Combine(env.ContentRootPath, "app_data/jobs/triggered/certes");

            webJobDeploymentTask = DeployWebJobScheduler(webJobPath, options)
                .ContinueWith(tsk =>
                {
                    webJobDeploymentTask = null;
                });

            return app;
        }

        private static async Task DeployWebJobScheduler(string webJobPath, IOptions<WebJobSchedulerOptions> options)
        {
            var dir = new DirectoryInfo(webJobPath);
            if (!dir.Exists)
            {
                dir.Create();
            }

            var assembly = typeof(CertesAzureApplicationBuilderExtensions).GetTypeInfo().Assembly;
            var prefix = $"{assembly.GetName().Name}.{WebJobFilePrefix}";
            var webJobFiles = assembly
                .GetManifestResourceNames()
                .Where(n => n.StartsWith(prefix))
                .ToArray();
            
            foreach (var file in webJobFiles)
            {
                var filename = file.Substring(prefix.Length);
                var dest = Path.Combine(webJobPath, filename);
                using (var destStream = File.Create(dest))
                using (var srcStream = assembly.GetManifestResourceStream(file))
                {
                    await srcStream.CopyToAsync(destStream);
                }
            }

            var optionsValue = options.Value;
            if (string.IsNullOrWhiteSpace(optionsValue.Schedule))
            {
                optionsValue.Schedule = "0 0 0 * * *";
            }

            var jobSettingsPath = Path.Combine(webJobPath, "settings.job");
            using (var destStream = File.Create(jobSettingsPath))
            using (var writer = new StreamWriter(destStream))
            {
                var content = JsonConvert.SerializeObject(optionsValue, Formatting.None, JsonUtil.CreateSettings());
                await writer.WriteAsync(content);
            }
        }
    }
}
