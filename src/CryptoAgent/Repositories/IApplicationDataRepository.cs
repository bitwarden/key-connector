using System.Threading.Tasks;

namespace Bit.CryptoAgent.Repositories
{
    public interface IApplicationDataRepository
    {
        Task<string> ReadSymmetricKeyAsync();
        Task UpdateSymmetricKeyAsync(string key);
    }
}