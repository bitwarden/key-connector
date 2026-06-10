using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Bit.KeyConnector;
using Bit.KeyConnector.Services.CertificateProviders;
using Bit.KeyConnector.Services.ClientFactories;
using KeyConnector.Tests.Helpers;
using NSubstitute;
using Xunit;

namespace KeyConnector.Tests.Services.CertificateProviders;

public class StoreCertificateProviderServiceTests
{
    private readonly IX509StoreFactory _storeFactory = Substitute.For<IX509StoreFactory>();

    [Fact]
    public async Task GetCertificateAsync_ReturnsCertificate_WhenThumbprintExists()
    {
        var cert = X509CertificateLoader.LoadPkcs12(TestCertificateData.PfxBytes, TestCertificateData.Password);
        var collection = new X509Certificate2Collection(cert);
        _storeFactory.FindByThumbprint(cert.Thumbprint).Returns(collection);
        var sut = new StoreCertificateProviderService(
            new KeyConnectorSettings { Certificate = new KeyConnectorSettings.CertificateSettings { StoreThumbprint = cert.Thumbprint } },
            _storeFactory);

        var result = await sut.GetCertificateAsync();

        Assert.NotNull(result);
        Assert.Equal(TestCertificateData.Thumbprint, result.Thumbprint);
        Assert.NotNull(result.GetRSAPublicKey());
        Assert.NotNull(result.GetRSAPrivateKey());
        _storeFactory.Received(1).FindByThumbprint(TestCertificateData.Thumbprint);
    }

    [Fact]
    public async Task GetCertificateAsync_ReturnsNull_WhenThumbprintNotFound()
    {
        _storeFactory.FindByThumbprint(Arg.Any<string>()).Returns(new X509Certificate2Collection());
        var sut = new StoreCertificateProviderService(
            new KeyConnectorSettings { Certificate = new KeyConnectorSettings.CertificateSettings { StoreThumbprint = TestCertificateData.Thumbprint } },
            _storeFactory);

        var result = await sut.GetCertificateAsync();

        Assert.Null(result);
        _storeFactory.Received(1).FindByThumbprint(TestCertificateData.Thumbprint);
    }

    [Theory]
    [InlineData("AB:CD:EF:01", "ABCDEF01")]
    [InlineData("ab cd ef 01", "ABCDEF01")]
    [InlineData("AB-CD-EF-01", "ABCDEF01")]
    [InlineData("abcdef01", "ABCDEF01")]
    public async Task GetCertificateAsync_CleansThumbprint_WhenThumbprintContainsSeparators(string input, string expected)
    {
        _storeFactory.FindByThumbprint(Arg.Any<string>()).Returns(new X509Certificate2Collection());
        var sut = new StoreCertificateProviderService(
            new KeyConnectorSettings { Certificate = new KeyConnectorSettings.CertificateSettings { StoreThumbprint = input } },
            _storeFactory);

        await sut.GetCertificateAsync();

        _storeFactory.Received(1).FindByThumbprint(expected);
    }
}
