using System;
using System.IO;
using System.Threading.Tasks;
using Bit.KeyConnector.Repositories;
using Bit.KeyConnector.Repositories.JsonFile;
using JsonFlatFileDataStore;

namespace KeyConnector.Tests.Repositories.JsonFile;

public class JsonFileFixture : IUserKeyRepositoryFixture
{
    private string _tempDir;
    private DataStore _dataStore;

    public IUserKeyRepository Repository { get; private set; }

    public Task InitializeAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"kc-json-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        var dbPath = Path.Combine(_tempDir, "database.json");
        _dataStore = new DataStore(dbPath, keyProperty: "--foobar--");
        Repository = new UserKeyRepository(_dataStore);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _dataStore?.Dispose();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        return Task.CompletedTask;
    }
}

public class UserKeyRepositoryTests : UserKeyRepositoryTestBase<JsonFileFixture>
{
    public UserKeyRepositoryTests(JsonFileFixture fixture) : base(fixture) { }
}
