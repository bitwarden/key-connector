using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Bit.KeyConnector.Services.ClientFactories;

namespace Bit.KeyConnector.Services.CertificateProviders
{
    public class AzureKeyVaultCertificateProviderService : ICertificateProviderService
    {
        private readonly KeyConnectorSettings _settings;
        private readonly IAzureKeyVaultClientFactory _keyVaultClientFactory;

        public AzureKeyVaultCertificateProviderService(KeyConnectorSettings settings,
            IAzureKeyVaultClientFactory keyVaultClientFactory)
        {
            _settings = settings;
            _keyVaultClientFactory = keyVaultClientFactory;
        }

        public async Task<X509Certificate2> GetCertificateAsync()
        {
            var credential = new ClientSecretCredential(_settings.Certificate.AzureKeyvaultAdTenantId,
                _settings.Certificate.AzureKeyvaultAdAppId, _settings.Certificate.AzureKeyvaultAdSecret);
            var keyVaultUri = new Uri(_settings.Certificate.AzureKeyvaultUri);

            var certificateClient = _keyVaultClientFactory.CreateCertificateClient(keyVaultUri, credential);
            var certificateResponse = await certificateClient.GetCertificateAsync(
                _settings.Certificate.AzureKeyvaultCertificateName);
            var certificate = certificateResponse.Value;
            if (certificate.Policy?.Exportable == true && certificate.Policy?.KeyType == CertificateKeyType.Rsa)
            {
                var secretName = ParseSecretName(certificate.SecretId);
                var secretClient = _keyVaultClientFactory.CreateSecretClient(keyVaultUri, credential);
                var secretResponse = await secretClient.GetSecretAsync(secretName);
                var secret = secretResponse.Value;
                if (string.Equals(secret.Properties.ContentType, CertificateContentType.Pkcs12.ToString(),
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    var pfxBytes = Convert.FromBase64String(secret.Value);
                    return new X509Certificate2(pfxBytes);
                }
            }
            return null;
        }

        private string ParseSecretName(Uri secretId)
        {
            if (secretId.Segments.Length < 3)
            {
                throw new InvalidOperationException($@"The secret ""{secretId}"" does not contain a valid name.");
            }
            return secretId.Segments[2].TrimEnd('/');
        }
    }
}
