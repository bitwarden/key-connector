using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Bit.KeyConnector.Repositories.EntityFramework
{
    public class SqlServerDatabaseContextFactory : IDesignTimeDbContextFactory<SqlServerDatabaseContext>
    {
        public SqlServerDatabaseContext CreateDbContext(string[] args)
        {
            return new SqlServerDatabaseContext(SettingsFactory.Settings);
        }
    }

    public class PostgreSqlDatabaseContextFactory : IDesignTimeDbContextFactory<PostgreSqlDatabaseContext>
    {
        public PostgreSqlDatabaseContext CreateDbContext(string[] args)
        {
            return new PostgreSqlDatabaseContext(SettingsFactory.Settings);
        }
    }

    public class MySqlDatabaseContextFactory : IDesignTimeDbContextFactory<MySqlDatabaseContext>
    {
        public MySqlDatabaseContext CreateDbContext(string[] args)
        {
            return new MySqlDatabaseContext(SettingsFactory.Settings);
        }
    }

    public class SqliteDatabaseContextFactory : IDesignTimeDbContextFactory<SqliteDatabaseContext>
    {
        public SqliteDatabaseContext CreateDbContext(string[] args)
        {
            return new SqliteDatabaseContext(SettingsFactory.Settings);
        }
    }

    public static class SettingsFactory
    {
        public static KeyConnectorSettings Settings { get; } = new KeyConnectorSettings();

        static SettingsFactory()
        {
            var configBuilder = new ConfigurationBuilder().AddUserSecrets<Startup>();
            var config = configBuilder.Build();
            ConfigurationBinder.Bind(config.GetSection("KeyConnectorSettings"), Settings);
        }
    }
}
