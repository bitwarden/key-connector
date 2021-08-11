using Google.Cloud.Kms.V1;
using Google.Protobuf;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Bit.CryptoAgent.Services
{
    public class GoogleCloudKmsRsaKeyService : IRsaKeyService
    {
        private readonly KeyManagementServiceClient _keyManagementServiceClient;
        private readonly CryptoKeyName _cryptoKeyName;
        private readonly CryptoKeyVersionName _cryptoKeyVersionName;

        public GoogleCloudKmsRsaKeyService(
            CryptoAgentSettings settings)
        {
            _keyManagementServiceClient = KeyManagementServiceClient.Create();
            _cryptoKeyName = new CryptoKeyName(settings.RsaKey.GoogleCloudProjectId,
                settings.RsaKey.GoogleCloudLocationId, settings.RsaKey.GoogleCloudKeyringId,
                settings.RsaKey.GoogleCloudKeyId);
            _cryptoKeyVersionName = new CryptoKeyVersionName(settings.RsaKey.GoogleCloudProjectId,
                settings.RsaKey.GoogleCloudLocationId, settings.RsaKey.GoogleCloudKeyringId,
                settings.RsaKey.GoogleCloudKeyId, settings.RsaKey.GoogleCloudKeyVersionId);
        }

        public async Task<byte[]> EncryptAsync(byte[] data)
        {
            var result = await _keyManagementServiceClient.EncryptAsync(_cryptoKeyName, ByteString.CopyFrom(data));
            return result.Ciphertext.ToByteArray();

        }

        public async Task<byte[]> DecryptAsync(byte[] data)
        {
            var result = await _keyManagementServiceClient.DecryptAsync(_cryptoKeyName, ByteString.CopyFrom(data));
            return result.Plaintext.ToByteArray();
        }

        public async Task<byte[]> SignAsync(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            var digest = new Digest
            {
                Sha256 = ByteString.CopyFrom(hash)
            };
            var result = await _keyManagementServiceClient.AsymmetricSignAsync(_cryptoKeyVersionName, digest);
            return result.Signature.ToByteArray();
        }

        public async Task<bool> VerifyAsync(byte[] data, byte[] signature)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            var rsa = await GetRsaPublicKeyAsync();
            var verified = rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return verified;
        }

        public async Task<byte[]> GetPublicKeyAsync()
        {
            var rsa = await GetRsaPublicKeyAsync();
            return rsa.ExportRSAPublicKey();
        }

        private async Task<RSA> GetRsaPublicKeyAsync()
        {
            var publicKey = await _keyManagementServiceClient.GetPublicKeyAsync(_cryptoKeyVersionName);
            var blocks = publicKey.Pem.Split("-", StringSplitOptions.RemoveEmptyEntries);
            var pem = Convert.FromBase64String(blocks[1]);
            var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(pem, out _);
            return rsa;
        }
    }
}
