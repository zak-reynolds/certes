using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using System;
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

        public CertesMiddleware(RequestDelegate next, IOptions<CertesOptions> optionsAccessor, ILoggerFactory loggerFactory)
        {
            this.next = next;
            this.logger = loggerFactory.CreateLogger<CertesMiddleware>();
            this.options = optionsAccessor.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            // 1.   Check SSL status
            await CheckCertificates();
            // 2.   Get reg data
            // 2.1  New reg
            // 3.   Check authz status
            // 4.   Do authz
            // 5.   Do cert
            await next.Invoke(context);
        }

        private async Task CheckCertificates()
        {
            var siteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
            var tenantId = "";
            var clientId = "";
            var clientSecret = "";
            var subscriptionId = "";
            var resourceGroup = "";

            var authContext = new AuthenticationContext($"https://login.windows.net/{tenantId}");
            var credential = new ClientCredential(clientId, clientSecret);
            var token = await authContext.AcquireTokenAsync("https://management.azure.com/", credential);
            var credentials = new TokenCredentials(token.AccessToken);
            
            using (var client = new Microsoft.Azure.Management.WebSites.WebSiteManagementClient(credentials)
            {
                SubscriptionId = subscriptionId
            })
            {
                var siteResp = await client.Sites.GetSiteHostNameBindingsWithHttpMessagesAsync(resourceGroup, siteName);
                //siteResp.Body.Value;
                // WEBSITE_SITE_NAME
                // WEBSITE_SLOT_NAME 
                //client.Sites.get
            }
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
