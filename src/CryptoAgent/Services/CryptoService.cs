using Bit.CryptoAgent.Repositories;
using System;
using System.Threading.Tasks;

namespace Bit.CryptoAgent.Services
{
    public class CryptoService : ICryptoService
    {
        private readonly IRsaKeyService _rsaKeyService;
        private readonly ICryptoFunctionService _cryptoFunctionService;
        private readonly IApplicationDataRepository _applicationDataRepository;

        private byte[] _symmetricKey;

        public CryptoService(
            IRsaKeyService rsaKeyService,
            ICryptoFunctionService cryptoFunctionService,
            IApplicationDataRepository applicationDataRepository)
        {
            _rsaKeyService = rsaKeyService;
            _cryptoFunctionService = cryptoFunctionService;
            _applicationDataRepository = applicationDataRepository;
        }

        // AES Decrypt

        public async Task<byte[]> AesDecryptAsync(byte[] data, byte[] key = null)
        {
            if (data == null)
            {
                return null;
            }
            if (key == null)
            {
                key = await GetSymmetricKeyAsync();
            }
            var plainData = await _cryptoFunctionService.AesGcmDecryptAsync(data, key);
            return plainData;
        }

        public async Task<string> AesDecryptToB64Async(byte[] data, byte[] key = null)
        {
            var plainData = await AesDecryptAsync(data, key);
            return Convert.ToBase64String(plainData);
        }

        public async Task<byte[]> AesDecryptAsync(string b64Data, byte[] key = null)
        {
            var data = Convert.FromBase64String(b64Data);
            var plainData = await AesDecryptAsync(data, key);
            return plainData;
        }

        public async Task<string> AesDecryptToB64Async(string b64Data, byte[] key = null)
        {
            var data = Convert.FromBase64String(b64Data);
            var plainData = await AesDecryptToB64Async(data, key);
            return plainData;
        }

        // AES Encrypt

        public async Task<byte[]> AesEncryptAsync(byte[] data, byte[] key = null)
        {
            if (data == null)
            {
                return null;
            }
            if (key == null)
            {
                key = await GetSymmetricKeyAsync();
            }
            var encData = await _cryptoFunctionService.AesGcmEncryptAsync(data, key);
            return encData;
        }

        public async Task<byte[]> AesEncryptAsync(string b64Data, byte[] key = null)
        {
            var data = Convert.FromBase64String(b64Data);
            var encData = await AesEncryptAsync(data, key);
            return encData;
        }

        public async Task<string> AesEncryptToB64Async(byte[] data, byte[] key = null)
        {
            var encData = await AesEncryptAsync(data, key);
            return Convert.ToBase64String(encData);
        }

        public async Task<string> AesEncryptToB64Async(string b64Data, byte[] key = null)
        {
            var encData = await AesEncryptAsync(b64Data, key);
            return Convert.ToBase64String(encData);
        }

        // Helpers

        private async Task<byte[]> GetSymmetricKeyAsync()
        {
            if (_symmetricKey == null)
            {
                var encKey = await _applicationDataRepository.ReadSymmetricKeyAsync();
                if (encKey != null)
                {
                    var decodedEncKey = Convert.FromBase64String(encKey);
                    _symmetricKey = await RsaDecryptAsync(decodedEncKey);
                }
                else
                {
                    _symmetricKey = await _cryptoFunctionService.GetRandomBytesAsync(32);
                    var decodedEncKey = await RsaEncryptAsync(_symmetricKey);
                    encKey = Convert.ToBase64String(decodedEncKey);
                    await _applicationDataRepository.UpdateSymmetricKeyAsync(encKey);
                }
            }

            return _symmetricKey;
        }

        private async Task<byte[]> RsaEncryptAsync(byte[] data)
        {
            if (data == null)
            {
                return null;
            }
            return await _rsaKeyService.EncryptAsync(data);
        }

        private async Task<byte[]> RsaDecryptAsync(byte[] data)
        {
            if (data == null)
            {
                return null;
            }
            return await _rsaKeyService.DecryptAsync(data);
        }
    }
}
