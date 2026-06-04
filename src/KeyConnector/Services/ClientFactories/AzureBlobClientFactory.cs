using Azure.Storage.Blobs;

namespace Bit.KeyConnector.Services.ClientFactories
{
    public class AzureBlobClientFactory : IAzureBlobClientFactory
    {
        public BlobContainerClient CreateBlobContainerClient(string connectionString, string containerName)
        {
            return new BlobContainerClient(connectionString, containerName);
        }
    }
}
