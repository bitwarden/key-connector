using System.Threading.Tasks;

namespace Bit.KeyConnector.Services
{
    public interface IRsaKeyService
    {
        Task<byte[]> DecryptAsync(byte[] data);
        Task<byte[]> EncryptAsync(byte[] data);
        Task<byte[]> SignAsync(byte[] data);
        Task<bool> VerifyAsync(byte[] data, byte[] signature);
        Task<byte[]> GetPublicKeyAsync();
    }
}
