using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bit.KeyConnector;
using Bit.KeyConnector.HostedServices;
using Bit.KeyConnector.Repositories;
using Bit.KeyConnector.Repositories.EntityFramework;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MariaDb;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;

namespace KeyConnector.Tests.Repositories.EntityFramework;

public static class ContainerImages
{
    public const string SqlServer = "mcr.microsoft.com/mssql/server:2022-latest";
    public const string PostgreSql = "postgres:14";
    public const string MySql = "mysql:8";
    public const string MariaDb = "mariadb:10";
    public const string Mongo = "mongo:7";
}

public abstract class EfFixtureBase : IUserKeyRepositoryFixture, IApplicationDataRepositoryFixture
{
    protected ServiceProvider ServiceProvider { get; private set; }

    public IUserKeyRepository Repository { get; private set; }
    public IApplicationDataRepository ApplicationDataRepository { get; private set; }

    protected abstract Task StartInfrastructureAsync();
    protected abstract void RegisterDbContext(IServiceCollection services, KeyConnectorSettings settings);
    protected abstract KeyConnectorSettings.DatabaseSettings CreateDatabaseSettings();

    public async Task InitializeAsync()
    {
        await StartInfrastructureAsync();

        var settings = new KeyConnectorSettings { Database = CreateDatabaseSettings() };

        var services = new ServiceCollection();
        services.AddSingleton(settings);
        RegisterDbContext(services, settings);
        services.AddSingleton<IUserKeyRepository, UserKeyRepository>();
        services.AddSingleton<IApplicationDataRepository, ApplicationDataRepository>();

        ServiceProvider = services.BuildServiceProvider();

        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        await dbContext.Database.EnsureCreatedAsync();

        Repository = ServiceProvider.GetRequiredService<IUserKeyRepository>();
        ApplicationDataRepository = ServiceProvider.GetRequiredService<IApplicationDataRepository>();
    }

    public async Task ClearApplicationDataAsync()
    {
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var existing = await dbContext.ApplicationDatas.ToListAsync();
        dbContext.ApplicationDatas.RemoveRange(existing);
        await dbContext.SaveChangesAsync();
    }

    public virtual Task DisposeAsync()
    {
        ServiceProvider?.Dispose();
        return Task.CompletedTask;
    }
}

public abstract class EfContainerFixtureBase<TContainer> : EfFixtureBase
    where TContainer : DockerContainer
{
    protected TContainer Container { get; private set; }

    protected abstract TContainer CreateContainer();

    protected override async Task StartInfrastructureAsync()
    {
        Container = CreateContainer();
        await Container.StartAsync();
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await Container.DisposeAsync();
    }
}

public class SqliteFixture : EfFixtureBase
{
    private string _tempDir;

    protected override Task StartInfrastructureAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"kc-sqlite-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        return Task.CompletedTask;
    }

    protected override KeyConnectorSettings.DatabaseSettings CreateDatabaseSettings() =>
        new()
        {
            Provider = "sqlite",
            SqliteConnectionString = $"Data Source={Path.Combine(_tempDir, "database.db")}"
        };

    protected override void RegisterDbContext(IServiceCollection services, KeyConnectorSettings settings) =>
        services.AddDbContext<DatabaseContext, SqliteDatabaseContext>();

    public override Task DisposeAsync()
    {
        base.DisposeAsync();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        return Task.CompletedTask;
    }
}

public class SqlServerFixture : EfContainerFixtureBase<MsSqlContainer>
{
    protected override MsSqlContainer CreateContainer() =>
        new MsSqlBuilder(ContainerImages.SqlServer).Build();

    protected override KeyConnectorSettings.DatabaseSettings CreateDatabaseSettings() =>
        new() { Provider = "sqlserver", SqlServerConnectionString = Container.GetConnectionString() };

    protected override void RegisterDbContext(IServiceCollection services, KeyConnectorSettings settings) =>
        services.AddDbContext<DatabaseContext, SqlServerDatabaseContext>();
}

public class PostgreSqlFixture : EfContainerFixtureBase<PostgreSqlContainer>
{
    protected override PostgreSqlContainer CreateContainer() =>
        new PostgreSqlBuilder(ContainerImages.PostgreSql).Build();

    protected override KeyConnectorSettings.DatabaseSettings CreateDatabaseSettings() =>
        new() { Provider = "postgresql", PostgreSqlConnectionString = Container.GetConnectionString() };

    protected override void RegisterDbContext(IServiceCollection services, KeyConnectorSettings settings) =>
        services.AddDbContext<DatabaseContext, PostgreSqlDatabaseContext>();
}

public class MySqlFixture : EfContainerFixtureBase<MySqlContainer>
{
    protected override MySqlContainer CreateContainer() =>
        new MySqlBuilder(ContainerImages.MySql).Build();

    protected override KeyConnectorSettings.DatabaseSettings CreateDatabaseSettings() =>
        new() { Provider = "mysql", MySqlConnectionString = Container.GetConnectionString() };

    protected override void RegisterDbContext(IServiceCollection services, KeyConnectorSettings settings) =>
        services.AddDbContext<DatabaseContext, MySqlDatabaseContext>();
}

public class MariaDbFixture : EfContainerFixtureBase<MariaDbContainer>
{
    protected override MariaDbContainer CreateContainer() =>
        new MariaDbBuilder(ContainerImages.MariaDb).Build();

    protected override KeyConnectorSettings.DatabaseSettings CreateDatabaseSettings() =>
        new() { Provider = "mysql", MySqlConnectionString = Container.GetConnectionString() };

    protected override void RegisterDbContext(IServiceCollection services, KeyConnectorSettings settings) =>
        services.AddDbContext<DatabaseContext, MySqlDatabaseContext>();
}

public class MongoFixture : EfContainerFixtureBase<MongoDbContainer>
{
    public const string DatabaseName = "kc-test";

    protected override MongoDbContainer CreateContainer() =>
        new MongoDbBuilder(ContainerImages.Mongo).Build();

    protected override KeyConnectorSettings.DatabaseSettings CreateDatabaseSettings() =>
        new()
        {
            Provider = "mongo",
            MongoConnectionString = Container.GetConnectionString(),
            MongoDatabaseName = DatabaseName
        };

    protected override void RegisterDbContext(IServiceCollection services, KeyConnectorSettings settings) =>
        services.AddDbContext<DatabaseContext, MongoDbDatabaseContext>();

    public IMongoCollection<BsonDocument> GetCollection(string name)
    {
        return new MongoClient(Container.GetConnectionString())
            .GetDatabase(DatabaseName)
            .GetCollection<BsonDocument>(name);
    }

    public async Task RunDataMigrationAsync()
    {
        var settings = new KeyConnectorSettings { Database = CreateDatabaseSettings() };
        var service = new MongoDataMigrationHostedService(
            settings, NullLogger<MongoDataMigrationHostedService>.Instance);
        await service.StartAsync(CancellationToken.None);
    }
}
