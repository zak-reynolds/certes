using Microsoft.Azure.Management.WebSites;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Rest;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Certes.AspNet.Azure
{
    public class AzureWebAppSslBindingManagerTests
    {
        private IServiceProvider serviceProvider;

        [Fact]
        public async Task CanGetHostNames()
        {
            BuildServiceProvider();
            var mgr = CreateBindingManager();
            Assert.IsType<WebAppSslBindingManager>(mgr);
            await mgr.GetHostNames();
        }

        [Fact]
        public async Task CanInstallCertificate()
        {
            const string password = "abcd1234";
            const string thumbprint = "‎f4d123b861e104f03d91767f10e315467620b96d";

            BuildServiceProvider();
            var pfx = File.ReadAllBytes("./Data/cert.pfx");

            ISslBindingManager mgr = CreateBindingManager();
            await mgr.InstallCertificate(thumbprint, pfx, password);
            var webAppOptions = serviceProvider.GetRequiredService<IOptions<WebAppOptions>>().Value;
            using (var client = await CreateClient())
            {
                var cert = await client.Certificates.GetCertificateAsync(webAppOptions.ResourceGroup, thumbprint);
                Assert.NotNull(cert);
                await client.Certificates.DeleteCertificateAsync(webAppOptions.ResourceGroup, thumbprint);
            }
        }

        private ISslBindingManager CreateBindingManager()
        {
            var mgr = serviceProvider.GetRequiredService<ISslBindingManager>();
            return mgr;
        }

        private void BuildServiceProvider()
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets("certes-dev")
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<ServicePrincipalOptions>(config.GetSection("certes:azure:servicePrincipal"));
            services.Configure<WebAppOptions>(config.GetSection("certes:azure:webApp"));

            services.AddTransient<IClientCredentialProvider, ServicePrincipalCredentialProvider>();
            services.AddTransient<ISslBindingManager, WebAppSslBindingManager>();

            this.serviceProvider = services.BuildServiceProvider();
        }

        private async Task<WebSiteManagementClient> CreateClient()
        {
            var credentialProvider = serviceProvider.GetRequiredService<IClientCredentialProvider>();
            var webApp = serviceProvider.GetRequiredService<IOptions<WebAppOptions>>();
            var token = await credentialProvider.GetOrCreateAccessToken();
            var credentials = new TokenCredentials(token);
            return new WebSiteManagementClient(credentials)
            {
                SubscriptionId = webApp.Value.SubscriptionId
            };
        }
    }
}
