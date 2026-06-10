using System;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;

namespace Bit.KeyConnector.Services.ClientFactories
{
    public class AzureKeyVaultClientFactory : IAzureKeyVaultClientFactory
    {
        public CertificateClient CreateCertificateClient(Uri vaultUri, ClientSecretCredential credential)
        {
            return new CertificateClient(vaultUri, credential);
        }

        public SecretClient CreateSecretClient(Uri vaultUri, ClientSecretCredential credential)
        {
            return new SecretClient(vaultUri, credential);
        }
    }
}
