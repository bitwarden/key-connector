using System;
using System.Linq;
using System.Threading.Tasks;
using Bit.KeyConnector.HostedServices;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace KeyConnector.Tests.Repositories.EntityFramework;

public class MongoMigrationTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fixture;

    public MongoMigrationTests(MongoFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CompletedMigration_DoesNotConvertNewLegacyDocuments()
    {
        await ResetAsync();
        await SeedUserKeyAsync(Guid.NewGuid(), GuidRepresentation.CSharpLegacy);

        await _fixture.RunDataMigrationAsync();

        // A document written in the old format after the migration completed should be left alone.
        await SeedUserKeyAsync(Guid.NewGuid(), GuidRepresentation.CSharpLegacy);
        await _fixture.RunDataMigrationAsync();

        Assert.Equal(1, await LegacyUserKeyCountAsync());
    }

    [Fact]
    public async Task RerunAfterPartialMigration_ConvertsRemainingAndLeavesConvertedDocuments()
    {
        await ResetAsync();
        // A previous run that failed partway would leave a mix of already-converted (standard) and not-yet-converted
        // (legacy) documents and no completion marker. Re-running must finish the job without a marker present.
        var alreadyConverted = Guid.NewGuid();
        var notYetConverted = Guid.NewGuid();
        await SeedUserKeyAsync(alreadyConverted, GuidRepresentation.Standard);
        await SeedUserKeyAsync(notYetConverted, GuidRepresentation.CSharpLegacy);

        await _fixture.RunDataMigrationAsync();

        Assert.NotNull(await _fixture.Repository.ReadAsync(alreadyConverted));
        Assert.NotNull(await _fixture.Repository.ReadAsync(notYetConverted));
        Assert.Equal(0, await LegacyUserKeyCountAsync());
    }

    private async Task ResetAsync()
    {
        await _fixture.GetCollection(MongoDataMigrationHostedService.MigrationCollectionName)
            .DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        await _fixture.GetCollection("UserKey").DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        await _fixture.GetCollection("ApplicationData").DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    }

    private async Task SeedUserKeyAsync(Guid id, GuidRepresentation representation)
    {
        await _fixture.GetCollection("UserKey").InsertOneAsync(new BsonDocument
        {
            { "_id", new BsonBinaryData(id, representation) },
            { "Key", "legacy-key" },
            { "CreationDate", DateTime.UtcNow },
            { "RevisionDate", BsonNull.Value },
            { "LastAccessDate", BsonNull.Value }
        });
    }

    private async Task<int> LegacyUserKeyCountAsync()
    {
        var documents = await _fixture.GetCollection("UserKey")
            .Find(FilterDefinition<BsonDocument>.Empty)
            .ToListAsync();
        return documents.Count(d =>
            d["_id"].IsBsonBinaryData && d["_id"].AsBsonBinaryData.SubType == BsonBinarySubType.UuidLegacy);
    }
}
