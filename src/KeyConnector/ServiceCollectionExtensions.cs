using System;
using System.Security.Claims;
using Bit.KeyConnector.Repositories;
using Bit.KeyConnector.Services;
using Bit.KeyConnector.Services.Pkcs11;
using IdentityModel;
using IdentityServer4.AccessTokenValidation;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bit.KeyConnector
{
    public static class ServiceCollectionExtensions
    {
        public static void AddRsaKeyProvider(
            this IServiceCollection services,
            KeyConnectorSettings settings)
        {
            var rsaKeyProvider = settings.RsaKey.Provider?.ToLowerInvariant();
            if (rsaKeyProvider == "certificate" || rsaKeyProvider == "pkcs11")
            {
                if (rsaKeyProvider == "certificate")
                {
                    services.AddSingleton<IRsaKeyService, LocalCertificateRsaKeyService>();
                }
                else if (rsaKeyProvider == "pkcs11")
                {
                    services.AddSingleton<IRsaKeyService, Pkcs11RsaKeyService>();
                    services.AddSingleton<IPkcs11InteropFactory, Pkcs11InteropFactory>();
                }

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

        public static bool AddDatabase(
            this IServiceCollection services,
            KeyConnectorSettings settings)
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

        public static void AddAuthentication(
            this IServiceCollection services,
            IWebHostEnvironment environment,
            KeyConnectorSettings settings)
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
                    options.RequireHttpsMetadata = !environment.IsDevelopment() &&
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

            if (environment.IsDevelopment())
            {
                Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
            }
        }

        public static void AddBaseServices(
            this IServiceCollection services)
        {
            services.AddSingleton<ICryptoFunctionService, CryptoFunctionService>();
            services.AddSingleton<ICryptoService, CryptoService>();
        }
    }
}
