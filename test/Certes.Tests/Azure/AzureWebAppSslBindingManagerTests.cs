using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Xunit;

namespace Certes.Azure
{
    public class AzureWebAppSslBindingManagerTests
    {
        [Fact(Skip = "In progress")]
        public async Task CanGetHostNames()
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets()
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<AzureWebAppManagementOptions>(config);
            
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<AzureWebAppManagementOptions>>();
            
            var mgr = new AzureWebAppSslBindingManager(options);
            await mgr.GetHostNames();
        }
    }
}
