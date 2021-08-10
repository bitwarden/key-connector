using Azure.Storage.Blobs;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Bit.CryptoAgent.Services
{
    public class AzureStorageCertificateProviderService : ICertificateProviderService
    {
        private readonly CryptoAgentSettings _settings;

        public AzureStorageCertificateProviderService(CryptoAgentSettings settings)
        {
            _settings = settings;
        }

        public async Task<X509Certificate2> GetCertificateAsync()
        {
            var container = new BlobContainerClient(_settings.Certificate.AzureStorageConnectionString,
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
