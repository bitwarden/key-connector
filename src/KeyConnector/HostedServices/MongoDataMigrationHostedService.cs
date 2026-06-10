using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Bit.KeyConnector.HostedServices
{
    // One-time, idempotent conversion of documents written by the old MongoDB.Driver repository layer into the
    // formats the EF Core provider expects: UserKey ids move from the old C# binary format to the current one, and
    // the single ApplicationData document moves from an ObjectId id to the int id the entity uses.
    //
    // A marker document in the "Migration" collection records completion so the conversion is not re-scanned on every
    // start. It is written only after a full successful pass, and each document conversion is idempotent, so a run
    // that fails partway simply continues on the next start. Key Connector runs as a single instance, so no
    // cross-node coordination is needed.
    public class MongoDataMigrationHostedService : IHostedService
    {
        public const string MigrationCollectionName = "Migration";
        public const string MigrationId = "userkey-applicationdata-id-format";

        private const string _userKeyCollectionName = "UserKey";
        private const string _applicationDataCollectionName = "ApplicationData";
        private const int _batchSize = 1000;

        private readonly KeyConnectorSettings _settings;
        private readonly ILogger<MongoDataMigrationHostedService> _logger;

        public MongoDataMigrationHostedService(
            KeyConnectorSettings settings,
            ILogger<MongoDataMigrationHostedService> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var database = new MongoClient(_settings.Database.MongoConnectionString)
                .GetDatabase(_settings.Database.MongoDatabaseName);
            var migrations = database.GetCollection<BsonDocument>(MigrationCollectionName);

            if (await IsCompletedAsync(migrations, cancellationToken))
            {
                return;
            }

            _logger.LogInformation("Migrating MongoDB id formats.");
            await MigrateUserKeyIdsAsync(database, cancellationToken);
            await MigrateApplicationDataIdAsync(database, cancellationToken);
            await MarkCompletedAsync(migrations, cancellationToken);
            _logger.LogInformation("MongoDB id format migration complete.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task MigrateUserKeyIdsAsync(IMongoDatabase database, CancellationToken cancellationToken)
        {
            var collection = database.GetCollection<BsonDocument>(_userKeyCollectionName);

            // First collect only the ids needing conversion (the subtype can't be filtered server-side), streaming
            // them so the whole collection is never held in memory at once.
            var legacyIds = new List<BsonValue>();
            var options = new FindOptions<BsonDocument, BsonDocument>
            {
                Projection = Builders<BsonDocument>.Projection.Include("_id"),
                BatchSize = _batchSize
            };
            using (var cursor = await collection.FindAsync(FilterDefinition<BsonDocument>.Empty, options, cancellationToken))
            {
                while (await cursor.MoveNextAsync(cancellationToken))
                {
                    foreach (var document in cursor.Current)
                    {
                        if (IsLegacyGuid(document["_id"]))
                        {
                            legacyIds.Add(document["_id"]);
                        }
                    }
                }
            }

            for (var i = 0; i < legacyIds.Count; i += _batchSize)
            {
                var batch = legacyIds.Skip(i).Take(_batchSize).ToList();
                var documents = await collection
                    .Find(Builders<BsonDocument>.Filter.In("_id", batch))
                    .ToListAsync(cancellationToken);

                var writes = new List<WriteModel<BsonDocument>>();
                foreach (var document in documents)
                {
                    var id = document["_id"];
                    if (!IsLegacyGuid(id))
                    {
                        continue;
                    }

                    var guid = id.AsBsonBinaryData.ToGuid(GuidRepresentation.CSharpLegacy);
                    var migrated = (BsonDocument)document.DeepClone();
                    migrated["_id"] = new BsonBinaryData(guid, GuidRepresentation.Standard);

                    writes.Add(new InsertOneModel<BsonDocument>(migrated));
                    writes.Add(new DeleteOneModel<BsonDocument>(Builders<BsonDocument>.Filter.Eq("_id", id)));
                }

                if (writes.Count > 0)
                {
                    await collection.BulkWriteAsync(writes, cancellationToken: cancellationToken);
                    _logger.LogDebug("Migrated {Count} UserKey ids to the current binary format.", writes.Count / 2);
                }
            }
        }

        private async Task MigrateApplicationDataIdAsync(IMongoDatabase database, CancellationToken cancellationToken)
        {
            var collection = database.GetCollection<BsonDocument>(_applicationDataCollectionName);
            var documents = await collection
                .Find(FilterDefinition<BsonDocument>.Empty)
                .ToListAsync(cancellationToken);

            foreach (var document in documents)
            {
                var id = document["_id"];
                if (!id.IsObjectId)
                {
                    continue;
                }

                var migrated = (BsonDocument)document.DeepClone();
                migrated["_id"] = 0;

                // _id is immutable, so the converted document is written under its new id and the old one removed.
                // Skipping the insert when the new id already exists keeps this safe to run again after a failed run.
                var newIdExists = await collection
                    .Find(Builders<BsonDocument>.Filter.Eq("_id", 0))
                    .AnyAsync(cancellationToken);
                if (!newIdExists)
                {
                    await collection.InsertOneAsync(migrated, cancellationToken: cancellationToken);
                }

                await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id), cancellationToken);
                _logger.LogDebug("Migrated the ApplicationData id to an int.");
            }
        }

        private async Task<bool> IsCompletedAsync(
            IMongoCollection<BsonDocument> migrations,
            CancellationToken cancellationToken)
        {
            return await migrations
                .Find(Builders<BsonDocument>.Filter.Eq("_id", MigrationId))
                .AnyAsync(cancellationToken);
        }

        private async Task MarkCompletedAsync(
            IMongoCollection<BsonDocument> migrations,
            CancellationToken cancellationToken)
        {
            var document = new BsonDocument
            {
                { "_id", MigrationId },
                { "completedAt", DateTime.UtcNow }
            };
            await migrations.InsertOneAsync(document, cancellationToken: cancellationToken);
        }

        private static bool IsLegacyGuid(BsonValue id)
        {
            return id.IsBsonBinaryData && id.AsBsonBinaryData.SubType == BsonBinarySubType.UuidLegacy;
        }
    }
}
