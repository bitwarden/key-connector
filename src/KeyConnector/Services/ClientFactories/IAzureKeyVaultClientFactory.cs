using System;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;

namespace Bit.KeyConnector.Services.ClientFactories
{
    public interface IAzureKeyVaultClientFactory
    {
        CertificateClient CreateCertificateClient(Uri vaultUri, ClientSecretCredential credential);
        SecretClient CreateSecretClient(Uri vaultUri, ClientSecretCredential credential);
    }
}
