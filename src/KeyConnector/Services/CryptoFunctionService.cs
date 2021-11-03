using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Bit.KeyConnector.Services
{
    public class CryptoFunctionService : ICryptoFunctionService
    {
        public async Task<byte[]> AesGcmEncryptAsync(byte[] data, byte[] key)
        {
            using var aes = new AesGcm(key);
            var iv = await GetRandomBytesAsync(AesGcm.NonceByteSizes.MaxSize);
            var tag = new byte[AesGcm.TagByteSizes.MaxSize];
            var encData = new byte[data.Length];

            aes.Encrypt(iv, data, encData, tag);

            var encResult = new byte[encData.Length + tag.Length + iv.Length];
            encData.CopyTo(encResult, 0);
            tag.CopyTo(encResult, encData.Length);
            iv.CopyTo(encResult, encData.Length + tag.Length);

            return encResult;
        }

        public Task<byte[]> AesGcmDecryptAsync(byte[] data, byte[] key)
        {
            using var aes = new AesGcm(key);
            var endDataLength = data.Length - AesGcm.TagByteSizes.MaxSize - AesGcm.NonceByteSizes.MaxSize;
            var encData = new ArraySegment<byte>(data, 0, endDataLength);
            var tag = new ArraySegment<byte>(data, endDataLength, AesGcm.TagByteSizes.MaxSize);
            var iv = new ArraySegment<byte>(data, endDataLength + AesGcm.TagByteSizes.MaxSize,
                AesGcm.NonceByteSizes.MaxSize);
            var plainData = new byte[endDataLength];

            aes.Decrypt(iv, encData, tag, plainData);

            return Task.FromResult(plainData);
        }

        public Task<byte[]> RsaEncryptAsync(byte[] data, byte[] publicKey)
        {
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKey, out var bytesRead);
            return RsaEncryptAsync(data, rsa);
        }

        public Task<byte[]> RsaEncryptAsync(byte[] data, RSA publicKey)
        {
            var encData = publicKey.Encrypt(data, RSAEncryptionPadding.OaepSHA1);
            return Task.FromResult(encData);
        }

        public Task<byte[]> RsaDecryptAsync(byte[] data, byte[] privateKey)
        {
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKey, out var bytesRead);
            return RsaDecryptAsync(data, rsa);
        }

        public Task<byte[]> RsaDecryptAsync(byte[] data, RSA privateKey)
        {
            var encData = privateKey.Decrypt(data, RSAEncryptionPadding.OaepSHA1);
            return Task.FromResult(encData);
        }

        public Task<bool> RsaVerifyAsync(byte[] data, byte[] signature, byte[] publicKey)
        {
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKey, out var bytesRead);
            return RsaVerifyAsync(data, signature, rsa);
        }

        public Task<bool> RsaVerifyAsync(byte[] data, byte[] signature, RSA publicKey)
        {
            var valid = publicKey.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Task.FromResult(valid);
        }

        public Task<byte[]> RsaSignAsync(byte[] data, byte[] privateKey)
        {
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKey, out var bytesRead);
            return RsaSignAsync(data, rsa);
        }

        public Task<byte[]> RsaSignAsync(byte[] data, RSA privateKey)
        {
            var signature = privateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Task.FromResult(signature);
        }

        public Task<byte[]> GetRandomBytesAsync(int size)
        {
            var bytes = new byte[size];
            RandomNumberGenerator.Fill(bytes);
            return Task.FromResult(bytes);
        }
    }
}
