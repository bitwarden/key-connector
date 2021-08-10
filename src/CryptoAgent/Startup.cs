using Bit.CryptoAgent.Repositories;
using Bit.CryptoAgent.Services;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;

namespace Bit.CryptoAgent
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            Configuration = configuration;
            Environment = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            var settings = new CryptoAgentSettings();
            ConfigurationBinder.Bind(Configuration.GetSection("CryptoAgentSettings"), settings);
            services.AddSingleton(s => settings);

            var rsaKeyProvider = settings.RsaKey.Provider?.ToLowerInvariant();
            if (rsaKeyProvider == "certificate")
            {
                services.AddSingleton<IRsaKeyService, LocalCertificateRsaKeyService>();

                if (!string.IsNullOrWhiteSpace(settings.Certificate?.StoreThumbprint))
                {
                    services.AddSingleton<ICertificateProviderService, StoreCertificateProviderService>();
                }
                else if (!string.IsNullOrWhiteSpace(settings.Certificate?.FilesystemPath))
                {
                    services.AddSingleton<ICertificateProviderService, FilesystemCertificateProviderService>();
                }
                else if (!string.IsNullOrWhiteSpace(settings.Certificate?.AzureStorageConnectionString))
                {
                    services.AddSingleton<ICertificateProviderService, AzureStorageCertificateProviderService>();
                }
                else if (!string.IsNullOrWhiteSpace(settings.Certificate?.AzureKeyvaultUri))
                {
                    services.AddSingleton<ICertificateProviderService, AzureKeyVaultCertificateProviderService>();
                }
                else
                {
                    throw new Exception("No certificate provider configured.");
                }
            }
            else if (rsaKeyProvider == "azure")
            {
                if (!string.IsNullOrWhiteSpace(settings.RsaKey?.AzureKeyvaultUri))
                {
                    services.AddSingleton<IRsaKeyService, AzureKeyVaultRsaKeyService>();
                }
                else
                {
                    throw new Exception("No azure key vault configured.");
                }
            }
            else
            {
                throw new Exception("Unknown rsa key provider.");
            }

            services.AddSingleton<ICryptoFunctionService, CryptoFunctionService>();
            services.AddSingleton<ICryptoService, CryptoService>();

            // JsonFlatFileDataStore
            if (!string.IsNullOrWhiteSpace(settings.Database?.JsonFilePath))
            {
                // Assign foobar to keyProperty in order to not use incrementing Id functionality
                services.AddSingleton<IDataStore>(new DataStore(settings.Database.JsonFilePath, keyProperty: "--foobar--"));
                services.AddSingleton<IApplicationDataRepository, Repositories.JsonFile.ApplicationDataRepository>();
                services.AddSingleton<IUserKeyRepository, Repositories.JsonFile.UserKeyRepository>();
            }
            else
            {
                throw new Exception("No database configured.");
            }

            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());
        }
    }
}
