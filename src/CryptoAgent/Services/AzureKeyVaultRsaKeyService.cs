using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using System;
using System.Threading.Tasks;

namespace Bit.CryptoAgent.Services
{
    public class AzureKeyVaultRsaKeyService : IRsaKeyService
    {
        private readonly CryptoAgentSettings _settings;
        private readonly ClientSecretCredential _credential;

        private KeyVaultKey _key;
        private CryptographyClient _cryptographyClient;

        public AzureKeyVaultRsaKeyService(
            CryptoAgentSettings settings)
        {
            _settings = settings;
            _credential = new ClientSecretCredential(_settings.RsaKey.AzureKeyvaultAdTenantId,
                _settings.RsaKey.AzureKeyvaultAdAppId, _settings.RsaKey.AzureKeyvaultAdSecret);
        }

        public async Task<byte[]> EncryptAsync(byte[] data)
        {
            var client = await GetCryptographyClientAsync();
            var result = await client.EncryptAsync(EncryptionAlgorithm.RsaOaep, data);
            return result.Ciphertext;
        }

        public async Task<byte[]> DecryptAsync(byte[] data)
        {
            var client = await GetCryptographyClientAsync();
            var result = await client.DecryptAsync(EncryptionAlgorithm.RsaOaep, data);
            return result.Plaintext;
        }

        public async Task<byte[]> SignAsync(byte[] data)
        {
            var client = await GetCryptographyClientAsync();
            var result = await client.SignAsync(SignatureAlgorithm.RS256, data);
            return result.Signature;
        }

        public async Task<bool> VerifyAsync(byte[] data, byte[] signature)
        {
            var client = await GetCryptographyClientAsync();
            var result = await client.VerifyDataAsync(SignatureAlgorithm.RS256, data, signature);
            return result.IsValid;
        }

        public async Task<byte[]> GetPublicKeyAsync()
        {
            var key = await GetKeyAsync();
            return key.Key.ToRSA().ExportRSAPublicKey();
        }

        private async Task<CryptographyClient> GetCryptographyClientAsync()
        {
            if (_cryptographyClient == null)
            {
                var key = await GetKeyAsync();
                _cryptographyClient = new CryptographyClient(key.Id, _credential);
            }
            return _cryptographyClient;
        }

        private async Task<KeyVaultKey> GetKeyAsync()
        {
            if (_key == null)
            {
                var keyVaultUri = new Uri(_settings.RsaKey.AzureKeyvaultUri);
                var keyClient = new KeyClient(keyVaultUri, _credential);
                _key = await keyClient.GetKeyAsync(_settings.RsaKey.AzureKeyvaultKeyName);
            }
            return _key;
        }
    }
}
