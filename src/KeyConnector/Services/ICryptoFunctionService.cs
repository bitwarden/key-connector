using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Bit.KeyConnector.Services
{
    public interface ICryptoFunctionService
    {
        Task<byte[]> AesGcmDecryptAsync(byte[] data, byte[] key);
        Task<byte[]> AesGcmEncryptAsync(byte[] data, byte[] key);
        Task<byte[]> RsaDecryptAsync(byte[] data, byte[] privateKey);
        Task<byte[]> RsaDecryptAsync(byte[] data, RSA privateKey);
        Task<byte[]> RsaEncryptAsync(byte[] data, byte[] publicKey);
        Task<byte[]> RsaEncryptAsync(byte[] data, RSA publicKey);
        Task<byte[]> RsaSignAsync(byte[] data, byte[] privateKey);
        Task<byte[]> RsaSignAsync(byte[] data, RSA privateKey);
        Task<bool> RsaVerifyAsync(byte[] data, byte[] signature, byte[] publicKey);
        Task<bool> RsaVerifyAsync(byte[] data, byte[] signature, RSA publicKey);
        Task<byte[]> GetRandomBytesAsync(int size);
    }
}
