using System;
using System.IO;
using System.Threading.Tasks;
using Bit.KeyConnector;
using Bit.KeyConnector.Repositories;
using Bit.KeyConnector.Repositories.EntityFramework;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MariaDb;
using Testcontainers.MsSql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;

namespace KeyConnector.Tests.Repositories.EntityFramework;

public abstract class EfFixtureBase : IUserKeyRepositoryFixture
{
    private ServiceProvider _serviceProvider;

    public IUserKeyRepository Repository { get; private set; }

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

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        await dbContext.Database.EnsureCreatedAsync();

        Repository = _serviceProvider.GetRequiredService<IUserKeyRepository>();
    }

    public virtual Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        return Task.CompletedTask;
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

public class SqlServerFixture : EfFixtureBase
{
    private readonly MsSqlContainer _container = new MsSqlBuilder(ContainerImages.SqlServer).Build();

    protected override async Task StartInfrastructureAsync() => await _container.StartAsync();

    protected override KeyConnectorSettings.DatabaseSettings CreateDatabaseSettings() =>
        new() { Provider = "sqlserver", SqlServerConnectionString = _container.GetConnectionString() };

    protected override void RegisterDbContext(IServiceCollection services, KeyConnectorSettings settings) =>
        services.AddDbContext<DatabaseContext, SqlServerDatabaseContext>();

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }
}

public class PostgreSqlFixture : EfFixtureBase
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(ContainerImages.PostgreSql).Build();

    protected override async Task StartInfrastructureAsync() => await _container.StartAsync();

    protected override KeyConnectorSettings.DatabaseSettings CreateDatabaseSettings() =>
        new() { Provider = "postgresql", PostgreSqlConnectionString = _container.GetConnectionString() };

    protected override void RegisterDbContext(IServiceCollection services, KeyConnectorSettings settings) =>
        services.AddDbContext<DatabaseContext, PostgreSqlDatabaseContext>();

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }
}

public class MySqlFixture : EfFixtureBase
{
    private readonly MySqlContainer _container = new MySqlBuilder(ContainerImages.MySql).Build();

    protected override async Task StartInfrastructureAsync() => await _container.StartAsync();

    protected override KeyConnectorSettings.DatabaseSettings CreateDatabaseSettings() =>
        new() { Provider = "mysql", MySqlConnectionString = _container.GetConnectionString() };

    protected override void RegisterDbContext(IServiceCollection services, KeyConnectorSettings settings) =>
        services.AddDbContext<DatabaseContext, MySqlDatabaseContext>();

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }
}

public class MariaDbFixture : EfFixtureBase
{
    private readonly MariaDbContainer _container = new MariaDbBuilder(ContainerImages.MariaDb).Build();

    protected override async Task StartInfrastructureAsync() => await _container.StartAsync();

    protected override KeyConnectorSettings.DatabaseSettings CreateDatabaseSettings() =>
        new() { Provider = "mysql", MySqlConnectionString = _container.GetConnectionString() };

    protected override void RegisterDbContext(IServiceCollection services, KeyConnectorSettings settings) =>
        services.AddDbContext<DatabaseContext, MySqlDatabaseContext>();

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }
}

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
