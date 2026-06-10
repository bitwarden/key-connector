using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Bit.KeyConnector.Services.ClientFactories;

namespace Bit.KeyConnector.Services.CertificateProviders
{
    public class AzureStorageCertificateProviderService : ICertificateProviderService
    {
        private readonly KeyConnectorSettings _settings;
        private readonly IAzureBlobClientFactory _blobClientFactory;

        public AzureStorageCertificateProviderService(KeyConnectorSettings settings,
            IAzureBlobClientFactory blobClientFactory)
        {
            _settings = settings;
            _blobClientFactory = blobClientFactory;
        }

        public async Task<X509Certificate2> GetCertificateAsync()
        {
            var container = _blobClientFactory.CreateBlobContainerClient(
                _settings.Certificate.AzureStorageConnectionString,
                _settings.Certificate.AzureStorageContainer);
            await container.CreateIfNotExistsAsync();
            var blobClient = container.GetBlobClient(_settings.Certificate.AzureStorageFileName);
            if (await blobClient.ExistsAsync())
            {
                using var stream = new MemoryStream();
                await blobClient.DownloadToAsync(stream);
                return new X509Certificate2(stream.ToArray(), _settings.Certificate.AzureStorageFilePassword);
            }
            return null;
        }
    }
}
