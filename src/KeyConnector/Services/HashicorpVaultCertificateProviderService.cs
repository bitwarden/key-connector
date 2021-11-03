using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace Bit.KeyConnector.Services
{
    public class HashicorpVaultCertificateProviderService : ICertificateProviderService
    {
        private readonly KeyConnectorSettings _settings;

        public HashicorpVaultCertificateProviderService(KeyConnectorSettings settings)
        {
            _settings = settings;
        }

        public async Task<X509Certificate2> GetCertificateAsync()
        {
            var authMethod = new TokenAuthMethodInfo(_settings.Certificate.VaultToken);
            var vaultClientSettings = new VaultClientSettings(_settings.Certificate.VaultServerUri, authMethod);
            var vaultClient = new VaultClient(vaultClientSettings);

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

            return null;
        }
    }
}
