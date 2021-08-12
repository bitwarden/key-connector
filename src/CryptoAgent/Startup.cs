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

                var certificateProvider = settings.Certificate.Provider?.ToLowerInvariant();
                if (certificateProvider == "store")
                {
                    services.AddSingleton<ICertificateProviderService, StoreCertificateProviderService>();
                }
                else if (certificateProvider == "filesystem")
                {
                    services.AddSingleton<ICertificateProviderService, FilesystemCertificateProviderService>();
                }
                else if (certificateProvider == "azurestorage")
                {
                    services.AddSingleton<ICertificateProviderService, AzureStorageCertificateProviderService>();
                }
                else if (certificateProvider == "azurekv")
                {
                    services.AddSingleton<ICertificateProviderService, AzureKeyVaultCertificateProviderService>();
                }
                else if (certificateProvider == "vault")
                {
                    services.AddSingleton<ICertificateProviderService, HashicorpVaultCertificateProviderService>();
                }
                else
                {
                    throw new Exception("Unknown certificate provider configured.");
                }
            }
            else if (rsaKeyProvider == "azurekv")
            {
                services.AddSingleton<IRsaKeyService, AzureKeyVaultRsaKeyService>();
            }
            else if (rsaKeyProvider == "gcpkms")
            {
                services.AddSingleton<IRsaKeyService, GoogleCloudKmsRsaKeyService>();
            }
            else if (rsaKeyProvider == "awskms")
            {
                services.AddSingleton<IRsaKeyService, AwsKmsRsaKeyService>();
            }
            else
            {
                throw new Exception("Unknown rsa key provider configured.");
            }

            services.AddSingleton<ICryptoFunctionService, CryptoFunctionService>();
            services.AddSingleton<ICryptoService, CryptoService>();

            var databaseProvider = settings.Database.Provider?.ToLowerInvariant();
            if (databaseProvider == "json")
            {
                // Assign foobar to keyProperty in order to not use incrementing Id functionality
                services.AddSingleton<IDataStore>(
                    new DataStore(settings.Database.JsonFilePath, keyProperty: "--foobar--"));
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
