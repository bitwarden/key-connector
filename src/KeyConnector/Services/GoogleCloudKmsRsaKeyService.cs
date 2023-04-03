using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Google.Cloud.Kms.V1;
using Google.Protobuf;

namespace Bit.KeyConnector.Services
{
    public class GoogleCloudKmsRsaKeyService : IRsaKeyService
    {
        private readonly KeyManagementServiceClient _keyManagementServiceClient;
        private readonly CryptoKeyVersionName _cryptoKeyVersionName;

        public GoogleCloudKmsRsaKeyService(
            KeyConnectorSettings settings)
        {
            _keyManagementServiceClient = KeyManagementServiceClient.Create();
            _cryptoKeyVersionName = new CryptoKeyVersionName(settings.RsaKey.GoogleCloudProjectId,
                settings.RsaKey.GoogleCloudLocationId, settings.RsaKey.GoogleCloudKeyringId,
                settings.RsaKey.GoogleCloudKeyId, settings.RsaKey.GoogleCloudKeyVersionId);
        }

        public async Task<byte[]> EncryptAsync(byte[] data)
        {
            var publicKey = await GetRsaPublicKeyAsync();
            var result = publicKey.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
            return result;
        }

        public async Task<byte[]> DecryptAsync(byte[] data)
        {
            var result = await _keyManagementServiceClient.AsymmetricDecryptAsync(_cryptoKeyVersionName, ByteString.CopyFrom(data));
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
            return rsa.ExportSubjectPublicKeyInfo();
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
