using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IO;
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
            services.Configure<AzureWebAppManagementOptions>(config.GetSection("certes"));
            
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<AzureWebAppManagementOptions>>();
            
            var mgr = new AzureWebAppSslBindingManager(options);
            await mgr.GetHostNames();
        }
    }
}
