using System;
using System.IO;
using System.Threading.Tasks;
using Bit.KeyConnector;
using Bit.KeyConnector.Repositories;
using Bit.KeyConnector.Repositories.EntityFramework;
using Microsoft.Extensions.DependencyInjection;

namespace KeyConnector.Tests.Repositories.EntityFramework;

public class SqliteFixture : IUserKeyRepositoryFixture
{
    private readonly string _tempDir;
    private ServiceProvider _serviceProvider;

    public IUserKeyRepository Repository { get; private set; }

    public SqliteFixture()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"kc-sqlite-test-{Guid.NewGuid()}");
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_tempDir);
        var dbPath = Path.Combine(_tempDir, "database.db");

        var settings = new KeyConnectorSettings
        {
            Database = new KeyConnectorSettings.DatabaseSettings
            {
                Provider = "sqlite",
                SqliteConnectionString = $"Data Source={dbPath}"
            }
        };

        var services = new ServiceCollection();
        services.AddSingleton(settings);
        services.AddDbContext<DatabaseContext, SqliteDatabaseContext>();
        services.AddSingleton<IUserKeyRepository, UserKeyRepository>();

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        await dbContext.Database.EnsureCreatedAsync();

        Repository = _serviceProvider.GetRequiredService<IUserKeyRepository>();
    }

    public Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        return Task.CompletedTask;
    }
}

public class UserKeyRepositoryTests : UserKeyRepositoryTestBase<SqliteFixture>
{
    public UserKeyRepositoryTests(SqliteFixture fixture) : base(fixture) { }
}
