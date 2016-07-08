using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Certes.Azure
{
    public class CertesMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger logger;
        private readonly CertesOptions options;
        private readonly ISslBindingManager bindingManager;

        public CertesMiddleware(
            RequestDelegate next,
            IOptions<CertesOptions> optionsAccessor,
            ILoggerFactory loggerFactory,
            ISslBindingManager bindingManager)
        {
            this.next = next;
            this.logger = loggerFactory.CreateLogger<CertesMiddleware>();
            this.options = optionsAccessor.Value;
            this.bindingManager = bindingManager;
        }

        public async Task Invoke(HttpContext context)
        {
            var bindingGroups = await CheckCertificates();
            if (bindingGroups.Count == 0)
            {
                // All host names have valid SSL
                return;
            }

            // 2.   Get reg data
            // 2.1  New reg
            // 3.   Check authz status
            // 4.   Do authz
            // 5.   Do cert
            await next.Invoke(context);
        }

        private async Task<IList<IList<SslBinding>>> CheckCertificates()
        {
            var hostNames = await bindingManager.GetHostNames();

            // TODO: Support grouping host names by user filters, and specific CN

            // Group host names so we don't hit the SAN limit
            var namePerCert = options.MaxSanPerCert + 1; // with common name
            var bindingGroup = hostNames
                .OrderBy(h => h.HostName) // The groups should not change for the same set of host names
                .Select((h, i) => new { Group = i / options.MaxSanPerCert, Binding = h })
                .GroupBy(g => g.Group, g => g.Binding);

            // Renew only if no cert, or the cert is about to expire
            var renewDate = DateTimeOffset.Now.Add(options.RenewBeforeExpire);
            bindingGroup = bindingGroup
                .Where(g => g.Any(b => b.CertificateThumbprint == null || b.CertificateExpires <= renewDate));

            return bindingGroup.Select(b => b.ToArray()).ToArray();
        }
    }

    public static class CertesExtensions
    {
        private const string WebJobFilePrefix = "Certes.Azure.Resources.WebJob.";

        public static IApplicationBuilder UseCertes(this IApplicationBuilder app)
        {
            app.Map("/.certes/renew", sub =>
            {
                sub.UseMiddleware<CertesMiddleware>();
            });
            
            return app;
        }

        public static IApplicationBuilder UseCertesChallengeHandler(this IApplicationBuilder app)
        {
            app.Map("/.well-known/acme-challenge", sub =>
            {
            });

            return app;
        }

        public static IApplicationBuilder UseCertesWebJob(this IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            var webJobPath = Path.Combine(env.ContentRootPath, "app_data/jobs/triggered/certes");

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
                    srcStream.CopyTo(destStream);
                }
            }

            return app;
        }
    }
}
