using Bit.KeyConnector.Models;
using JsonFlatFileDataStore;
using System;
using System.Threading.Tasks;

namespace Bit.KeyConnector.Repositories.JsonFile
{
    public class UserKeyRepository : Repository<UserKeyModel, Guid>, IUserKeyRepository
    {
        public UserKeyRepository(IDataStore dataStore)
            : base(dataStore, "userKey")
        { }

        public override async Task CreateAsync(UserKeyModel item)
        {
            var collection = DataStore.GetCollection<JsonUserKeyModel>(CollectionName);
            await collection.InsertOneAsync(new JsonUserKeyModel(item));
        }

        // New model is required since JsonFlatFileDataStore doesn't handle Guid id types
        public class JsonUserKeyModel : BaseUserKeyModel
        {
            public JsonUserKeyModel() { }

            public JsonUserKeyModel(UserKeyModel model)
            {
                Id = model.Id.ToString();
                Key = model.Key;
                CreationDate = model.CreationDate;
                RevisionDate = model.RevisionDate;
                LastAccessDate = model.LastAccessDate;
            }

            public string Id { get; set; }
        }
    }
}
