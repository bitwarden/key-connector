using Bit.CryptoAgent.Repositories;
using Bit.CryptoAgent.Services;
using IdentityModel;
using IdentityServer4.AccessTokenValidation;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Globalization;
using System.Security.Claims;

namespace Bit.CryptoAgent
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
            var settings = new CryptoAgentSettings();
            ConfigurationBinder.Bind(Configuration.GetSection("CryptoAgentSettings"), settings);
            services.AddSingleton(s => settings);

            AddAuthentication(services, settings);
            AddRsaKeyProvider(services, settings);

            services.AddSingleton<ICryptoFunctionService, CryptoFunctionService>();
            services.AddSingleton<ICryptoService, CryptoService>();

            var efDatabaseProvider = AddDatabase(services, settings);

            services.AddControllers();

            if(efDatabaseProvider)
            {
                services.AddHostedService<HostedServices.DatabaseMigrationHostedService>();
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());
        }

        private bool AddDatabase(IServiceCollection services, CryptoAgentSettings settings)
        {
            var databaseProvider = settings.Database.Provider?.ToLowerInvariant();
            var efDatabaseProvider = databaseProvider == "sqlserver" || databaseProvider == "postgresql" ||
                databaseProvider == "mysql" || databaseProvider == "sqlite";

            if (databaseProvider == "json")
            {
                // Assign foobar to keyProperty in order to not use incrementing Id functionality
                services.AddSingleton<IDataStore>(
                    new DataStore(settings.Database.JsonFilePath, keyProperty: "--foobar--"));
                services.AddSingleton<IApplicationDataRepository, Repositories.JsonFile.ApplicationDataRepository>();
                services.AddSingleton<IUserKeyRepository, Repositories.JsonFile.UserKeyRepository>();
            }
            else if (databaseProvider == "mongo")
            {
                services.AddSingleton<IApplicationDataRepository, Repositories.Mongo.ApplicationDataRepository>();
                services.AddSingleton<IUserKeyRepository, Repositories.Mongo.UserKeyRepository>();
            }
            else if (efDatabaseProvider)
            {
                if (databaseProvider == "sqlserver")
                {
                    services.AddDbContext<Repositories.EntityFramework.DatabaseContext,
                        Repositories.EntityFramework.SqlServerDatabaseContext>();
                }
                else if (databaseProvider == "postgresql")
                {
                    services.AddDbContext<Repositories.EntityFramework.DatabaseContext,
                        Repositories.EntityFramework.PostgreSqlDatabaseContext>();
                }
                else if (databaseProvider == "mysql")
                {
                    services.AddDbContext<Repositories.EntityFramework.DatabaseContext,
                        Repositories.EntityFramework.MySqlDatabaseContext>();
                }
                else if (databaseProvider == "sqlite")
                {
                    services.AddDbContext<Repositories.EntityFramework.DatabaseContext,
                        Repositories.EntityFramework.SqliteDatabaseContext>();
                }
                services.AddSingleton<IApplicationDataRepository,
                    Repositories.EntityFramework.ApplicationDataRepository>();
                services.AddSingleton<IUserKeyRepository, Repositories.EntityFramework.UserKeyRepository>();
            }
            else
            {
                throw new Exception("No database configured.");
            }

            return efDatabaseProvider;
        }

        private void AddRsaKeyProvider(IServiceCollection services, CryptoAgentSettings settings)
        {
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
        }

        private void AddAuthentication(IServiceCollection services, CryptoAgentSettings settings)
        {
            services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity = new ClaimsIdentityOptions
                {
                    UserNameClaimType = JwtClaimTypes.Email,
                    UserIdClaimType = JwtClaimTypes.Subject
                };
            });
            services
                .AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = settings.IdentityServerUri;
                    options.RequireHttpsMetadata = !Environment.IsDevelopment() &&
                        settings.IdentityServerUri.StartsWith("https");
                    options.NameClaimType = ClaimTypes.Email;
                    options.SupportedTokens = SupportedTokens.Jwt;
                });

            services
                .AddAuthorization(config =>
                {
                    config.AddPolicy("Application", policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        policy.RequireClaim(JwtClaimTypes.AuthenticationMethod, "Application", "external");
                        policy.RequireClaim(JwtClaimTypes.Scope, "api");
                    });
                });

            if (Environment.IsDevelopment())
            {
                Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
            }
        }
    }
}
