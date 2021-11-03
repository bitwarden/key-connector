using System.Threading.Tasks;

namespace Bit.KeyConnector.Repositories
{
    public interface IApplicationDataRepository
    {
        Task<string> ReadSymmetricKeyAsync();
        Task UpdateSymmetricKeyAsync(string key);
    }
}