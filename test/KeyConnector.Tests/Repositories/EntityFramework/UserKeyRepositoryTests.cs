using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using Xunit;

namespace KeyConnector.Tests.Repositories.EntityFramework;

public class SqliteUserKeyRepositoryTests : UserKeyRepositoryTestBase<SqliteFixture>
{
    public SqliteUserKeyRepositoryTests(SqliteFixture fixture) : base(fixture) { }
}

public class SqlServerUserKeyRepositoryTests : UserKeyRepositoryTestBase<SqlServerFixture>
{
    public SqlServerUserKeyRepositoryTests(SqlServerFixture fixture) : base(fixture) { }
}

public class PostgreSqlUserKeyRepositoryTests : UserKeyRepositoryTestBase<PostgreSqlFixture>
{
    public PostgreSqlUserKeyRepositoryTests(PostgreSqlFixture fixture) : base(fixture) { }
}

public class MySqlUserKeyRepositoryTests : UserKeyRepositoryTestBase<MySqlFixture>
{
    public MySqlUserKeyRepositoryTests(MySqlFixture fixture) : base(fixture) { }
}

public class MariaDbUserKeyRepositoryTests : UserKeyRepositoryTestBase<MariaDbFixture>
{
    public MariaDbUserKeyRepositoryTests(MariaDbFixture fixture) : base(fixture) { }
}

public class MongoUserKeyRepositoryTests : UserKeyRepositoryTestBase<MongoFixture>
{
    public MongoUserKeyRepositoryTests(MongoFixture fixture) : base(fixture) { }
}

public class MongoUserKeyBackwardCompatibilityTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fixture;

    public MongoUserKeyBackwardCompatibilityTests(MongoFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LegacyUserKey_IsReadableAfterMigration()
    {
        var id = Guid.NewGuid();
        var creationDate = new DateTime(2024, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc);
        var collection = _fixture.GetCollection("UserKey");
        await collection.InsertOneAsync(new BsonDocument
        {
            { "_id", new BsonBinaryData(id, GuidRepresentation.CSharpLegacy) },
            { "Key", "legacy-key" },
            { "CreationDate", creationDate },
            { "RevisionDate", BsonNull.Value },
            { "LastAccessDate", BsonNull.Value }
        });

        await _fixture.RunDataMigrationAsync();

        var result = await _fixture.Repository.ReadAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("legacy-key", result.Key);
        Assert.Equal(creationDate, result.CreationDate);
    }
}
