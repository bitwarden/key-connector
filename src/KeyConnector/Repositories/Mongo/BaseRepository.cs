using MongoDB.Driver;

namespace Bit.KeyConnector.Repositories.Mongo
{
    public abstract class BaseRepository<T>
    {
        public BaseRepository(KeyConnectorSettings settings, string collectionName)
        {
            Client = new MongoClient(settings.Database.MongoConnectionString);
            Database = Client.GetDatabase(settings.Database.MongoDatabaseName);
            Collection = Database.GetCollection<T>(collectionName);
        }

        private MongoClient Client { get; set; }
        private IMongoDatabase Database { get; set; }
        protected IMongoCollection<T> Collection { get; private set; }
    }
}
