using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Bit.KeyConnector;
using Bit.KeyConnector.Services.CertificateProviders;
using Bit.KeyConnector.Services.ClientFactories;
using KeyConnector.Tests.Helpers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VaultSharp;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;
using Xunit;

namespace KeyConnector.Tests.Services.CertificateProviders;

public class HashicorpVaultCertificateProviderServiceTests
{
    private readonly IHashicorpVaultClientFactory _vaultClientFactory = Substitute.For<IHashicorpVaultClientFactory>();
    private readonly IVaultClient _vaultClient = Substitute.For<IVaultClient>();
    private readonly IKeyValueSecretsEngineV2 _kvV2 = Substitute.For<IKeyValueSecretsEngineV2>();
    private readonly ILogger<HashicorpVaultCertificateProviderService> _logger =
        Substitute.For<ILogger<HashicorpVaultCertificateProviderService>>();

    private HashicorpVaultCertificateProviderService CreateSut(KeyConnectorSettings settings = null)
    {
        settings ??= CreateSettings();
        _vaultClientFactory.CreateClient(Arg.Any<string>(), Arg.Any<string>()).Returns(_vaultClient);
        _vaultClient.V1.Secrets.KeyValue.V2.Returns(_kvV2);
        return new HashicorpVaultCertificateProviderService(settings, _logger, _vaultClientFactory);
    }

    [Fact]
    public async Task GetCertificateAsync_ReturnsCertificate_WhenSecretKeyExists()
    {
        var secret = CreateSecretData("cert-key", Convert.ToBase64String(TestCertificateData.PfxBytes));
        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns(secret);
        var sut = CreateSut();

        var cert = await sut.GetCertificateAsync();

        Assert.NotNull(cert);
        Assert.Equal(TestCertificateData.Thumbprint, cert.Thumbprint);
        Assert.NotNull(cert.GetRSAPublicKey());
        Assert.NotNull(cert.GetRSAPrivateKey());
        _vaultClientFactory.Received(1).CreateClient("https://vault.example.com", "test-token");
        await _kvV2.Received(1).ReadSecretAsync("secret/path", Arg.Any<int?>(), null);
    }

    [Fact]
    public async Task GetCertificateAsync_PassesCustomMountPoint_WhenMountPointIsConfigured()
    {
        var settings = CreateSettings(mountPoint: "custom/mount");
        var secret = CreateSecretData("cert-key", Convert.ToBase64String(TestCertificateData.PfxBytes));
        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns(secret);
        var sut = CreateSut(settings);

        await sut.GetCertificateAsync();

        await _kvV2.Received(1).ReadSecretAsync("secret/path", Arg.Any<int?>(), "custom/mount");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetCertificateAsync_PassesNullMountPoint_WhenMountPointIsBlankOrNull(string mountPoint)
    {
        var settings = CreateSettings(mountPoint: mountPoint);
        var secret = CreateSecretData("cert-key", Convert.ToBase64String(TestCertificateData.PfxBytes));
        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns(secret);
        var sut = CreateSut(settings);

        await sut.GetCertificateAsync();

        await _kvV2.Received(1).ReadSecretAsync("secret/path", Arg.Any<int?>(), null);
    }

    [Fact]
    public async Task GetCertificateAsync_ReturnsNull_WhenSecretKeyNotFound()
    {
        var secret = CreateSecretData("wrong-key", Convert.ToBase64String(TestCertificateData.PfxBytes));
        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns(secret);
        var sut = CreateSut();

        var cert = await sut.GetCertificateAsync();

        Assert.Null(cert);
    }

    [Fact]
    public async Task GetCertificateAsync_ReturnsNull_WhenSecretDataIsNull()
    {
        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns((Secret<SecretData>)null);
        var sut = CreateSut();

        var cert = await sut.GetCertificateAsync();

        Assert.Null(cert);
    }

    private static Secret<SecretData> CreateSecretData(string key, string value) =>
        new()
        {
            Data = new SecretData
            {
                Data = new Dictionary<string, object> { [key] = value }
            }
        };

    private static KeyConnectorSettings CreateSettings(
        string mountPoint = null) =>
        new()
        {
            Certificate = new KeyConnectorSettings.CertificateSettings
            {
                VaultServerUri = "https://vault.example.com",
                VaultToken = "test-token",
                VaultSecretMountPoint = mountPoint,
                VaultSecretPath = "secret/path",
                VaultSecretDataKey = "cert-key",
                VaultSecretFilePassword = TestCertificateData.Password
            }
        };
}
