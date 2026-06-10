using Bit.KeyConnector.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Bit.KeyConnector.Repositories.EntityFramework
{
    public abstract class DatabaseContext : DbContext
    {
        public DbSet<ApplicationData> ApplicationDatas { get; set; }
        public DbSet<UserKey> UserKeys { get; set; }
    }

    public class SqlServerDatabaseContext : DatabaseContext
    {
        private readonly KeyConnectorSettings _settings;

        public SqlServerDatabaseContext(KeyConnectorSettings settings)
        {
            _settings = settings;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlServer(_settings.Database.SqlServerConnectionString);
    }

    public class PostgreSqlDatabaseContext : DatabaseContext
    {
        private readonly KeyConnectorSettings _settings;

        public PostgreSqlDatabaseContext(KeyConnectorSettings settings)
        {
            _settings = settings;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseNpgsql(_settings.Database.PostgreSqlConnectionString);
    }

    public class SqliteDatabaseContext : DatabaseContext
    {
        private readonly KeyConnectorSettings _settings;

        public SqliteDatabaseContext(KeyConnectorSettings settings)
        {
            _settings = settings;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite(_settings.Database.SqliteConnectionString);
    }

    public class MySqlDatabaseContext : DatabaseContext
    {
        private readonly KeyConnectorSettings _settings;

        public MySqlDatabaseContext(KeyConnectorSettings settings)
        {
            _settings = settings;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseMySql(_settings.Database.MySqlConnectionString,
                ServerVersion.AutoDetect(_settings.Database.MySqlConnectionString));
    }

    public class MongoDbDatabaseContext : DatabaseContext
    {
        private readonly KeyConnectorSettings _settings;

        public MongoDbDatabaseContext(KeyConnectorSettings settings)
        {
            _settings = settings;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseMongoDB(_settings.Database.MongoConnectionString,
                _settings.Database.MongoDatabaseName);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserKey>().ToCollection("UserKey");
            modelBuilder.Entity<ApplicationData>().ToCollection("ApplicationData");
        }
    }

    public class ApplicationData
    {
        public int Id { get; set; }
        public string SymmetricKey { get; set; }
    }

    public class UserKey : UserKeyModel
    {
        public UserKey() { }

        public UserKey(UserKeyModel model)
        {
            Load(model);
        }

        public void Load(UserKeyModel model)
        {
            Id = model.Id;
            Key = model.Key;
            RevisionDate = model.RevisionDate;
            CreationDate = model.CreationDate;
            LastAccessDate = model.LastAccessDate;
        }

        public UserKeyModel ToUserKeyModel()
        {
            return new UserKeyModel
            {
                Id = Id,
                Key = Key,
                RevisionDate = RevisionDate,
                CreationDate = CreationDate,
                LastAccessDate = LastAccessDate
            };
        }
    }
}
