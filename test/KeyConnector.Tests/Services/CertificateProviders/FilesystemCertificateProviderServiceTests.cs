using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Bit.KeyConnector;
using Bit.KeyConnector.Services.CertificateProviders;
using KeyConnector.Tests.Helpers;
using Xunit;

namespace KeyConnector.Tests.Services.CertificateProviders;

public class FilesystemCertificateProviderServiceTests
{
    [Fact]
    public async Task GetCertificateAsync_ReturnsCertificate_WhenFileExists()
    {
        var settings = CreateSettings(TestCertificateData.PfxFilePath, TestCertificateData.Password);
        var sut = new FilesystemCertificateProviderService(settings);

        var cert = await sut.GetCertificateAsync();

        Assert.NotNull(cert);
        Assert.Equal(TestCertificateData.Thumbprint, cert.Thumbprint);
        Assert.NotNull(cert.GetRSAPublicKey());
        Assert.NotNull(cert.GetRSAPrivateKey());
    }

    [Fact]
    public async Task GetCertificateAsync_ReturnsCertificate_WhenFileHasNoPassword()
    {
        var settings = CreateSettings(TestCertificateData.NoPasswordPfxFilePath, null);
        var sut = new FilesystemCertificateProviderService(settings);

        var cert = await sut.GetCertificateAsync();

        Assert.NotNull(cert);
        Assert.Equal(TestCertificateData.NoPasswordThumbprint, cert.Thumbprint);
        Assert.NotNull(cert.GetRSAPublicKey());
        Assert.NotNull(cert.GetRSAPrivateKey());
    }

    [Fact]
    public async Task GetCertificateAsync_ThrowsException_WhenFileDoesNotExist()
    {
        var settings = CreateSettings("/nonexistent/path.pfx", TestCertificateData.Password);
        var sut = new FilesystemCertificateProviderService(settings);

        await Assert.ThrowsAnyAsync<Exception>(() => sut.GetCertificateAsync());
    }

    [Fact]
    public async Task GetCertificateAsync_ThrowsException_WhenPasswordIsWrong()
    {
        var settings = CreateSettings(TestCertificateData.PfxFilePath, "wrongpassword");
        var sut = new FilesystemCertificateProviderService(settings);

        await Assert.ThrowsAsync<CryptographicException>(() => sut.GetCertificateAsync());
    }

    private static KeyConnectorSettings CreateSettings(string path, string password) =>
        new()
        {
            Certificate = new KeyConnectorSettings.CertificateSettings
            {
                FilesystemPath = path,
                FilesystemPassword = password
            }
        };
}
