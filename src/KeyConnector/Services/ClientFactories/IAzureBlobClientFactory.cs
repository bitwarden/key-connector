using Azure.Storage.Blobs;

namespace Bit.KeyConnector.Services.ClientFactories
{
    public interface IAzureBlobClientFactory
    {
        BlobContainerClient CreateBlobContainerClient(string connectionString, string containerName);
    }
}
