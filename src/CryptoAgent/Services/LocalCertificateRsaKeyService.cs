using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Bit.CryptoAgent.Services
{
    public class LocalCertificateRsaKeyService : IRsaKeyService
    {
        private readonly ICertificateProviderService _certificateProviderService;
        private readonly ICryptoFunctionService _cryptoFunctionService;

        private X509Certificate2 _certificate;

        public LocalCertificateRsaKeyService(
            ICertificateProviderService certificateProviderService,
            ICryptoFunctionService cryptoFunctionService)
        {
            _certificateProviderService = certificateProviderService;
            _cryptoFunctionService = cryptoFunctionService;
        }

        public async Task<byte[]> EncryptAsync(byte[] data)
        {
            if (data == null)
            {
                return null;
            }
            var encData = await _cryptoFunctionService.RsaEncryptAsync(data, await GetPublicKeyAsync());
            return encData;
        }

        public async Task<byte[]> DecryptAsync(byte[] data)
        {
            if (data == null)
            {
                return null;
            }
            var plainData = await _cryptoFunctionService.RsaDecryptAsync(data, await GetPrivateKeyAsync());
            return plainData;
        }

        public async Task<byte[]> SignAsync(byte[] data)
        {
            if (data == null)
            {
                return null;
            }
            return await _cryptoFunctionService.RsaSignAsync(data, await GetPrivateKeyAsync());
        }

        public async Task<bool> VerifyAsync(byte[] data, byte[] signature)
        {
            if (data == null || signature == null)
            {
                return false;
            }
            return await _cryptoFunctionService.RsaVerifyAsync(data, signature, await GetPublicKeyAsync());
        }

        public async Task<byte[]> GetPublicKeyAsync()
        {
            var certificate = await GetCertificateAsync();
            return certificate.GetRSAPublicKey().ExportSubjectPublicKeyInfo();
        }

        private async Task<X509Certificate2> GetCertificateAsync()
        {
            if (_certificate == null)
            {
                _certificate = await _certificateProviderService.GetCertificateAsync();
            }

            return _certificate;
        }

        private async Task<System.Security.Cryptography.RSA> GetPrivateKeyAsync()
        {
            var certificate = await GetCertificateAsync();
            return certificate.GetRSAPrivateKey();
        }
    }
}
