using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Bit.KeyConnector.Services.ClientFactories;
using Microsoft.Extensions.Logging;

namespace Bit.KeyConnector.Services.CertificateProviders
{
    public class HashicorpVaultCertificateProviderService : ICertificateProviderService
    {
        private readonly KeyConnectorSettings _settings;
        private readonly ILogger<HashicorpVaultCertificateProviderService> _logger;
        private readonly IHashicorpVaultClientFactory _vaultClientFactory;

        public HashicorpVaultCertificateProviderService(KeyConnectorSettings settings,
            ILogger<HashicorpVaultCertificateProviderService> logger,
            IHashicorpVaultClientFactory vaultClientFactory)
        {
            _settings = settings;
            _logger = logger;
            _vaultClientFactory = vaultClientFactory;
        }

        public async Task<X509Certificate2> GetCertificateAsync()
        {
            var vaultClient = _vaultClientFactory.CreateClient(
                _settings.Certificate.VaultServerUri,
                _settings.Certificate.VaultToken);

            var mountPoint = string.IsNullOrWhiteSpace(_settings.Certificate.VaultSecretMountPoint) ?
                null : _settings.Certificate.VaultSecretMountPoint;
            var secret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                path: _settings.Certificate.VaultSecretPath,
                mountPoint: mountPoint);

            if (secret?.Data?.Data?.ContainsKey(_settings.Certificate.VaultSecretDataKey) ?? false)
            {
                var certData = secret.Data.Data[_settings.Certificate.VaultSecretDataKey] as string;
                return new X509Certificate2(Convert.FromBase64String(certData),
                    _settings.Certificate.VaultSecretFilePassword);
            }
            else
            {
                _logger.LogError("No secret found in Hashicorp Vault with key {key}", _settings.Certificate.VaultSecretDataKey);
            }

            return null;
        }
    }
}
