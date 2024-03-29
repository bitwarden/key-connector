using System.Globalization;
using Bit.KeyConnector.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Bit.KeyConnector
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            Configuration = configuration;
            Environment = env;

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            var settings = new KeyConnectorSettings();
            ConfigurationBinder.Bind(Configuration.GetSection("KeyConnectorSettings"), settings);
            services.AddSingleton(s => settings);

            services.AddAuthentication(Environment, settings);
            services.AddRsaKeyProvider(settings);
            services.AddBaseServices();
            var efDatabaseProvider = services.AddDatabase(settings);

            services.AddControllers();

            services.AddHostedService<HostedServices.TransferToHostedService>();
            if (efDatabaseProvider)
            {
                services.AddHostedService<HostedServices.DatabaseMigrationHostedService>();
            }

            if (!settings.RsaKey.AwsUseSymmetricEncryption)
            {
                services.AddHealthChecks()
                    .AddCheck<RsaHealthCheckService>("RsaHealthCheckService");
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, KeyConnectorSettings settings)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(policy => policy.SetIsOriginAllowed(o => o == settings.WebVaultUri.TrimEnd('/'))
                .AllowAnyMethod().AllowAnyHeader().AllowCredentials());

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();

                if (!settings.RsaKey.AwsUseSymmetricEncryption)
                {
                    endpoints.MapHealthChecks("~/health").AllowAnonymous();
                }
            });
        }
    }
}
