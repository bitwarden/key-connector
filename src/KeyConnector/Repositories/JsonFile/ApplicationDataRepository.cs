using JsonFlatFileDataStore;
using System.Threading.Tasks;

namespace Bit.KeyConnector.Repositories.JsonFile
{
    public class ApplicationDataRepository : IApplicationDataRepository
    {
        public ApplicationDataRepository(IDataStore dataStore)
        {
            DataStore = dataStore;
        }

        protected IDataStore DataStore { get; private set; }

        public Task<string> ReadSymmetricKeyAsync()
        {
            var item = DataStore.GetItem("symmetricKey");
            return Task.FromResult(item as string);
        }

        public async Task UpdateSymmetricKeyAsync(string key)
        {
            await DataStore.ReplaceItemAsync("symmetricKey", key, true);
        }
    }
}
