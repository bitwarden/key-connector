using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Bit.KeyConnector;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace KeyConnector.Tests.Helpers;

public class KeyConnectorWebApplicationFactory : WebApplicationFactory<Startup>
{
    private readonly string _tempDir;
    private readonly string _pfxPath;
    private readonly string _dbPath;
    private const string PfxPassword = "test";

    public KeyConnectorWebApplicationFactory()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"kc-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        _pfxPath = Path.Combine(_tempDir, "test.pfx");
        _dbPath = Path.Combine(_tempDir, "database.db");

        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=KeyConnectorTest", rsa, HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
        File.WriteAllBytes(_pfxPath, cert.Export(X509ContentType.Pfx, PfxPassword));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["keyConnectorSettings:database:provider"] = "sqlite",
                ["keyConnectorSettings:database:sqliteConnectionString"] = $"Data Source={_dbPath}",
                ["keyConnectorSettings:rsaKey:provider"] = "certificate",
                ["keyConnectorSettings:certificate:provider"] = "filesystem",
                ["keyConnectorSettings:certificate:filesystemPath"] = _pfxPath,
                ["keyConnectorSettings:certificate:filesystemPassword"] = PfxPassword,
                ["keyConnectorSettings:identityServerUri"] = "http://localhost",
                ["keyConnectorSettings:webVaultUri"] = "http://localhost",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtTestHelper.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = JwtTestHelper.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtTestHelper.AuthenticationScheme;
                })
                .AddJwtBearer(JwtTestHelper.AuthenticationScheme, options =>
                {
                    options.Authority = JwtTestHelper.TestIssuer;
                    options.RequireHttpsMetadata = false;
                    options.BackchannelHttpHandler = JwtTestHelper.CreateDiscoveryHandler();
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = ClaimTypes.Email,
                        ValidateAudience = false,
                    };
                });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
