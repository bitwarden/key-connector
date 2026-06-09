using System.Threading.Tasks;
using Bit.KeyConnector.Repositories;
using Xunit;

namespace KeyConnector.Tests.Repositories;

public interface IApplicationDataRepositoryFixture : IAsyncLifetime
{
    IApplicationDataRepository ApplicationDataRepository { get; }

    Task ClearApplicationDataAsync();
}

public abstract class ApplicationDataRepositoryTestBase<TFixture> : IClassFixture<TFixture>, IAsyncLifetime
    where TFixture : class, IApplicationDataRepositoryFixture
{
    protected readonly TFixture Fixture;
    protected readonly IApplicationDataRepository Repository;

    protected ApplicationDataRepositoryTestBase(TFixture fixture)
    {
        Fixture = fixture;
        Repository = fixture.ApplicationDataRepository;
    }

    public async Task InitializeAsync()
    {
        // ApplicationData is a single-record store shared across a class's tests, so reset it before each one.
        await Fixture.ClearApplicationDataAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SymmetricKey_IsNullUntilSet_ThenStoredAndOverwritten()
    {
        Assert.Null(await Repository.ReadSymmetricKeyAsync());

        await Repository.UpdateSymmetricKeyAsync("initial-key");
        Assert.Equal("initial-key", await Repository.ReadSymmetricKeyAsync());

        await Repository.UpdateSymmetricKeyAsync("rotated-key");
        Assert.Equal("rotated-key", await Repository.ReadSymmetricKeyAsync());
    }
}
