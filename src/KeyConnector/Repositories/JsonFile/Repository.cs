using System;
using System.Linq;
using System.Threading.Tasks;
using Bit.KeyConnector.Models;
using JsonFlatFileDataStore;

namespace Bit.KeyConnector.Repositories.JsonFile
{
    public class Repository<TItem, TId> : IRepository<TItem, TId>
        where TId : IEquatable<TId>
        where TItem : class, IStoredItem<TId>
    {
        public Repository(
            IDataStore dataStore,
            string collectionName)
        {
            DataStore = dataStore;
            CollectionName = collectionName;
        }

        protected IDataStore DataStore { get; private set; }
        protected string CollectionName { get; private set; }

        public virtual async Task CreateAsync(TItem item)
        {
            var collection = DataStore.GetCollection<TItem>(CollectionName);
            await collection.InsertOneAsync(item);
        }

        public virtual Task<TItem> ReadAsync(TId id)
        {
            var collection = DataStore.GetCollection<TItem>(CollectionName);
            var item = collection.AsQueryable().FirstOrDefault(i => i.Id.Equals(id));
            return Task.FromResult(item);
        }

        public virtual async Task UpdateAsync(TItem item)
        {
            var collection = DataStore.GetCollection<TItem>(CollectionName);
            await collection.ReplaceOneAsync(item.Id, item);
        }

        public virtual async Task DeleteAsync(TId id)
        {
            var collection = DataStore.GetCollection<TItem>(CollectionName);
            await collection.DeleteOneAsync(id);
        }
    }
}
