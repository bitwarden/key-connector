using System;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Bit.KeyConnector;
using Bit.KeyConnector.Services.CertificateProviders;
using Bit.KeyConnector.Services.ClientFactories;
using KeyConnector.Tests.Helpers;
using NSubstitute;
using Xunit;

namespace KeyConnector.Tests.Services.CertificateProviders;

public class AzureKeyVaultCertificateProviderServiceTests
{
    private readonly IAzureKeyVaultClientFactory _keyVaultClientFactory =
        Substitute.For<IAzureKeyVaultClientFactory>();
    private readonly CertificateClient _certificateClient = Substitute.For<CertificateClient>();
    private readonly SecretClient _secretClient = Substitute.For<SecretClient>();

    private AzureKeyVaultCertificateProviderService CreateSut()
    {
        var settings = new KeyConnectorSettings
        {
            Certificate = new KeyConnectorSettings.CertificateSettings
            {
                AzureKeyvaultUri = "https://vault.azure.net",
                AzureKeyvaultCertificateName = "test-cert",
                AzureKeyvaultAdTenantId = "tenant-id",
                AzureKeyvaultAdAppId = "app-id",
                AzureKeyvaultAdSecret = "app-secret"
            }
        };
        _keyVaultClientFactory
            .CreateCertificateClient(Arg.Any<Uri>(), Arg.Any<ClientSecretCredential>())
            .Returns(_certificateClient);
        _keyVaultClientFactory
            .CreateSecretClient(Arg.Any<Uri>(), Arg.Any<ClientSecretCredential>())
            .Returns(_secretClient);
        return new AzureKeyVaultCertificateProviderService(settings, _keyVaultClientFactory);
    }

    [Fact]
    public async Task GetCertificateAsync_ReturnsCertificate_WhenExportableRsaPkcs12()
    {
        SetupCertificateResponse(exportable: true, keyType: CertificateKeyType.Rsa);
        SetupSecretResponse("my-secret", CertificateContentType.Pkcs12.ToString(),
            Convert.ToBase64String(TestCertificateData.NoPasswordPfxBytes));
        var sut = CreateSut();

        var cert = await sut.GetCertificateAsync();

        Assert.NotNull(cert);
        Assert.Equal(TestCertificateData.NoPasswordThumbprint, cert.Thumbprint);
        Assert.NotNull(cert.GetRSAPublicKey());
        Assert.NotNull(cert.GetRSAPrivateKey());
        _keyVaultClientFactory.Received(1).CreateCertificateClient(
            new Uri("https://vault.azure.net"),
            Arg.Is<ClientSecretCredential>(c => GetNonPublicProperty(c, "TenantId") == "tenant-id"
                && GetNonPublicProperty(c, "ClientId") == "app-id"
                && GetNonPublicProperty(c, "ClientSecret") == "app-secret"));
        await _certificateClient.Received(1).GetCertificateAsync("test-cert", Arg.Any<CancellationToken>());
        _keyVaultClientFactory.Received(1).CreateSecretClient(
            new Uri("https://vault.azure.net"),
            Arg.Is<ClientSecretCredential>(c => GetNonPublicProperty(c, "TenantId") == "tenant-id"
                && GetNonPublicProperty(c, "ClientId") == "app-id"
                && GetNonPublicProperty(c, "ClientSecret") == "app-secret"));
        await _secretClient.Received(1).GetSecretAsync("my-secret", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCertificateAsync_ReturnsNull_WhenNotExportable()
    {
        SetupCertificateResponse(exportable: false, keyType: CertificateKeyType.Rsa);
        var sut = CreateSut();

        var cert = await sut.GetCertificateAsync();

        Assert.Null(cert);
    }

    [Fact]
    public async Task GetCertificateAsync_ReturnsNull_WhenKeyTypeNotRsa()
    {
        SetupCertificateResponse(exportable: true, keyType: CertificateKeyType.Ec);
        var sut = CreateSut();

        var cert = await sut.GetCertificateAsync();

        Assert.Null(cert);
    }

    [Fact]
    public async Task GetCertificateAsync_ReturnsNull_WhenContentTypeNotPkcs12()
    {
        SetupCertificateResponse(exportable: true, keyType: CertificateKeyType.Rsa);
        SetupSecretResponse("my-secret", "application/x-pem-file", Convert.ToBase64String(TestCertificateData.PfxBytes));
        var sut = CreateSut();

        var cert = await sut.GetCertificateAsync();

        Assert.Null(cert);
    }

    [Fact]
    public async Task GetCertificateAsync_ReturnsNull_WhenPolicyIsNull()
    {
        var kvCert = CertificateModelFactory.KeyVaultCertificateWithPolicy(
            CertificateModelFactory.CertificateProperties(
                new Uri("https://vault.azure.net/certificates/test-cert")),
            policy: null);
        _certificateClient.GetCertificateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(kvCert, Substitute.For<Response>()));
        var sut = CreateSut();

        var cert = await sut.GetCertificateAsync();

        Assert.Null(cert);
    }

    [Fact]
    public async Task GetCertificateAsync_ThrowsException_WhenSecretIdHasTooFewSegments()
    {
        var secretId = new Uri("https://vault.azure.net/");
        SetupCertificateResponse(exportable: true, keyType: CertificateKeyType.Rsa, secretId: secretId);
        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetCertificateAsync());
        Assert.Contains("does not contain a valid name", exception.Message);
    }

    private void SetupCertificateResponse(bool exportable, CertificateKeyType keyType,
        Uri secretId = null)
    {
        secretId ??= new Uri("https://vault.azure.net/secrets/my-secret/version1");
        var policy = new CertificatePolicy("Self", "CN=Test")
        {
            Exportable = exportable,
            KeyType = keyType
        };
        var properties = CertificateModelFactory.CertificateProperties(
            new Uri("https://vault.azure.net/certificates/test-cert"));
        var kvCert = CertificateModelFactory.KeyVaultCertificateWithPolicy(
            properties, secretId: secretId, policy: policy);
        _certificateClient.GetCertificateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(kvCert, Substitute.For<Response>()));
    }

    private void SetupSecretResponse(string secretName, string contentType, string value)
    {
        var secretProperties = SecretModelFactory.SecretProperties(
            new Uri($"https://vault.azure.net/secrets/{secretName}"));
        secretProperties.ContentType = contentType;
        var kvSecret = SecretModelFactory.KeyVaultSecret(secretProperties, value);
        _secretClient.GetSecretAsync(secretName, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(kvSecret, Substitute.For<Response>()));
    }

    // ClientSecretCredential does not expose TenantId, ClientId, ClientSecret as public properties
    private static string GetNonPublicProperty(object obj, string name) =>
        (string)obj.GetType()
            .GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(obj);
}
