using System.Threading.Tasks;
using Bit.KeyConnector;
using Bit.KeyConnector.Repositories;
using Testcontainers.MongoDb;

namespace KeyConnector.Tests.Repositories.Mongo;

public class MongoFixture : IUserKeyRepositoryFixture
{
    private readonly MongoDbContainer _container = new MongoDbBuilder(ContainerImages.Mongo).Build();

    public IUserKeyRepository Repository { get; private set; }

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var settings = new KeyConnectorSettings
        {
            Database = new KeyConnectorSettings.DatabaseSettings
            {
                MongoConnectionString = _container.GetConnectionString(),
                MongoDatabaseName = "kc-test"
            }
        };

        Repository = new Bit.KeyConnector.Repositories.Mongo.UserKeyRepository(settings);
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

public class UserKeyRepositoryTests : UserKeyRepositoryTestBase<MongoFixture>
{
    public UserKeyRepositoryTests(MongoFixture fixture) : base(fixture) { }
}
