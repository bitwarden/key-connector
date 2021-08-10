using System.Threading.Tasks;

namespace Bit.CryptoAgent.Services
{
    public interface ICryptoService
    {
        Task<byte[]> AesDecryptAsync(byte[] data, byte[] key = null);
        Task<byte[]> AesDecryptAsync(string b64Data, byte[] key = null);
        Task<string> AesDecryptToB64Async(byte[] data, byte[] key = null);
        Task<string> AesDecryptToB64Async(string b64Data, byte[] key = null);
        Task<byte[]> AesEncryptAsync(byte[] data, byte[] key = null);
        Task<byte[]> AesEncryptAsync(string b64Data, byte[] key = null);
        Task<string> AesEncryptToB64Async(byte[] data, byte[] key = null);
        Task<string> AesEncryptToB64Async(string b64Data, byte[] key = null);
        Task<byte[]> RsaEncryptAsync(byte[] data, byte[] publicKey = null);
        Task<byte[]> RsaDecryptAsync(byte[] data);
        Task<bool> RsaVerifyAsync(byte[] data, byte[] signature, byte[] publicKey = null);
    }
}