using System;
using System.Threading.Tasks;
using Bit.KeyConnector.Repositories;
using Bit.KeyConnector.Services.RsaKey;
using Microsoft.Extensions.Logging;

namespace Bit.KeyConnector.Services.Crypto
{
    public class CryptoService : ICryptoService
    {
        private readonly IRsaKeyService _rsaKeyService;
        private readonly ICryptoFunctionService _cryptoFunctionService;
        private readonly IApplicationDataRepository _applicationDataRepository;
        private readonly ILogger<CryptoService> _logger;

        private byte[] _symmetricKey;

        public CryptoService(
            IRsaKeyService rsaKeyService,
            ICryptoFunctionService cryptoFunctionService,
            IApplicationDataRepository applicationDataRepository,
            ILogger<CryptoService> logger)
        {
            _rsaKeyService = rsaKeyService;
            _cryptoFunctionService = cryptoFunctionService;
            _applicationDataRepository = applicationDataRepository;
            _logger = logger;
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
                    _logger.LogDebug("Found a stored encrypted symmetric key; decrypting it with the RSA key.");
                    var decodedEncKey = Convert.FromBase64String(encKey);
                    _symmetricKey = await RsaDecryptAsync(decodedEncKey);
                }
                else
                {
                    _logger.LogDebug("No stored symmetric key found; generating a new one and saving it to the repository.");
                    var newSymmetricKey = await _cryptoFunctionService.GetRandomBytesAsync(32);
                    var decodedEncKey = await RsaEncryptAsync(newSymmetricKey);

                    if (decodedEncKey == null)
                    {
                        throw new Exception("RSA encryption failed. Your RSA key may not be configured properly.");
                    }

                    encKey = Convert.ToBase64String(decodedEncKey);
                    await _applicationDataRepository.UpdateSymmetricKeyAsync(encKey);

                    // Only save in memory after successfully saving to database
                    _symmetricKey = newSymmetricKey;
                }
            }
            else
            {
                _logger.LogDebug("Using the symmetric key already cached in memory.");
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
