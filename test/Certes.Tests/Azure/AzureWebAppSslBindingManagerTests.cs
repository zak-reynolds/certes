using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace Certes.Azure
{
    public class AzureWebAppSslBindingManagerTests
    {
        [Fact]
        public async Task CanGetHostNames()
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets("certes-dev")
                .AddEnvironmentVariables()
                .Build();
            
            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<AzureManagementClientOptions>(config.GetSection("certes"));
            services.Configure<AzureWebAppOptions>(config.GetSection("certes"));

            services.AddTransient<IAzureClientCredentialProvider, AzureClientCredentialProvider>();
            services.AddTransient<ISslBindingManager, AzureWebAppSslBindingManager>();

            var serviceProvider = services.BuildServiceProvider();
            
            var mgr = serviceProvider.GetRequiredService<ISslBindingManager>();
            Assert.IsType<AzureWebAppSslBindingManager>(mgr);
            await mgr.GetHostNames();
        }
    }
}
