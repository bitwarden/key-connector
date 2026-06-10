using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace KeyConnector.Tests.Repositories.EntityFramework;

public class SqliteApplicationDataRepositoryTests : ApplicationDataRepositoryTestBase<SqliteFixture>
{
    public SqliteApplicationDataRepositoryTests(SqliteFixture fixture) : base(fixture) { }
}

public class SqlServerApplicationDataRepositoryTests : ApplicationDataRepositoryTestBase<SqlServerFixture>
{
    public SqlServerApplicationDataRepositoryTests(SqlServerFixture fixture) : base(fixture) { }
}

public class PostgreSqlApplicationDataRepositoryTests : ApplicationDataRepositoryTestBase<PostgreSqlFixture>
{
    public PostgreSqlApplicationDataRepositoryTests(PostgreSqlFixture fixture) : base(fixture) { }
}

public class MySqlApplicationDataRepositoryTests : ApplicationDataRepositoryTestBase<MySqlFixture>
{
    public MySqlApplicationDataRepositoryTests(MySqlFixture fixture) : base(fixture) { }
}

public class MariaDbApplicationDataRepositoryTests : ApplicationDataRepositoryTestBase<MariaDbFixture>
{
    public MariaDbApplicationDataRepositoryTests(MariaDbFixture fixture) : base(fixture) { }
}

public class MongoApplicationDataRepositoryTests : ApplicationDataRepositoryTestBase<MongoFixture>
{
    public MongoApplicationDataRepositoryTests(MongoFixture fixture) : base(fixture) { }

    [Fact]
    public async Task LegacyObjectIdApplicationData_IsReadableAndUpdatableAfterMigration()
    {
        var collection = Fixture.GetCollection("ApplicationData");
        await collection.InsertOneAsync(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "SymmetricKey", "legacy-symmetric-key" }
        });

        await Fixture.RunDataMigrationAsync();

        var read = await Repository.ReadSymmetricKeyAsync();
        Assert.Equal("legacy-symmetric-key", read);

        await Repository.UpdateSymmetricKeyAsync("rotated-key");

        var updated = await Repository.ReadSymmetricKeyAsync();
        Assert.Equal("rotated-key", updated);

        // The migration replaced the ObjectId id with the int the entity uses, leaving a single document.
        var documents = await collection.Find(new BsonDocument()).ToListAsync();
        var document = Assert.Single(documents);
        Assert.Equal(0, document["_id"].AsInt32);
    }
}
