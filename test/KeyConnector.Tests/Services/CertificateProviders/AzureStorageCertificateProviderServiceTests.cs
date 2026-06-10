using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Bit.KeyConnector;
using Bit.KeyConnector.Services.CertificateProviders;
using Bit.KeyConnector.Services.ClientFactories;
using KeyConnector.Tests.Helpers;
using NSubstitute;
using Xunit;

namespace KeyConnector.Tests.Services.CertificateProviders;

public class AzureStorageCertificateProviderServiceTests
{
    private readonly IAzureBlobClientFactory _blobClientFactory = Substitute.For<IAzureBlobClientFactory>();
    private readonly BlobContainerClient _containerClient = Substitute.For<BlobContainerClient>();
    private readonly BlobClient _blobClient = Substitute.For<BlobClient>();

    private AzureStorageCertificateProviderService CreateSut()
    {
        var settings = new KeyConnectorSettings
        {
            Certificate = new KeyConnectorSettings.CertificateSettings
            {
                AzureStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=test",
                AzureStorageContainer = "certs",
                AzureStorageFileName = "cert.pfx",
                AzureStorageFilePassword = TestCertificateData.Password
            }
        };
        _blobClientFactory
            .CreateBlobContainerClient(Arg.Any<string>(), Arg.Any<string>())
            .Returns(_containerClient);
        _containerClient.GetBlobClient(Arg.Any<string>()).Returns(_blobClient);
        return new AzureStorageCertificateProviderService(settings, _blobClientFactory);
    }

    [Fact]
    public async Task GetCertificateAsync_ReturnsCertificate_WhenBlobExists()
    {
        _blobClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(true, Substitute.For<Response>()));
        _blobClient.DownloadToAsync(Arg.Any<Stream>())
            .Returns(callInfo =>
            {
                callInfo.Arg<Stream>().Write(TestCertificateData.PfxBytes);
                return Substitute.For<Response>();
            });
        var sut = CreateSut();

        var cert = await sut.GetCertificateAsync();

        Assert.NotNull(cert);
        Assert.Equal(TestCertificateData.Thumbprint, cert.Thumbprint);
        Assert.NotNull(cert.GetRSAPublicKey());
        Assert.NotNull(cert.GetRSAPrivateKey());
        _blobClientFactory.Received(1).CreateBlobContainerClient(
            "DefaultEndpointsProtocol=https;AccountName=test", "certs");
        await _containerClient.Received(1).CreateIfNotExistsAsync();
        _containerClient.Received(1).GetBlobClient("cert.pfx");
        await _blobClient.Received(1).DownloadToAsync(Arg.Any<Stream>());
    }

    [Fact]
    public async Task GetCertificateAsync_ReturnsNull_WhenBlobDoesNotExist()
    {
        _blobClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(false, Substitute.For<Response>()));
        var sut = CreateSut();

        var cert = await sut.GetCertificateAsync();

        Assert.Null(cert);
        await _containerClient.Received(1).CreateIfNotExistsAsync();
    }
}
