using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Certes.Examples.AppService
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                builder.AddUserSecrets();
            }

            builder.AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                builder.AddApplicationInsightsSettings(developerMode: true);
            }

            Configuration = builder.Build();
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry(Configuration);
            
            services.AddCertes(certes =>
            {
                certes
                    .AddInMemoryProviders()
                    .UseConfiguration(this.Configuration)
                    .UseAzureConfiguration(this.Configuration);
            });
        }
        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            env.ConfigureNLog("nlog.config");
            loggerFactory.AddNLog();

            if (env.IsDevelopment())
            {
                loggerFactory.AddConsole();
                app.UseDeveloperExceptionPage();
            }

            app.UseApplicationInsightsRequestTelemetry();
            app.UseApplicationInsightsExceptionTelemetry();
            
            app.UseCertes()
                .UseCertesHttpChallengeResponder()
                .UseCertesWebJobScheduler();

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
