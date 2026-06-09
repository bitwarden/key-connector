using System;
using System.Threading.Tasks;
using Bit.KeyConnector.Models;
using Bit.KeyConnector.Repositories;
using Xunit;

namespace KeyConnector.Tests.Repositories;

public interface IUserKeyRepositoryFixture : IAsyncLifetime
{
    IUserKeyRepository Repository { get; }
}

public abstract class UserKeyRepositoryTestBase<TFixture> : IClassFixture<TFixture>
    where TFixture : class, IUserKeyRepositoryFixture
{
    private readonly IUserKeyRepository _repository;

    protected UserKeyRepositoryTestBase(TFixture fixture)
    {
        _repository = fixture.Repository;
    }

    [Fact]
    public async Task CreateAsync_ThenReadAsync_ReturnsAllFields()
    {
        var userId = Guid.NewGuid();
        var creationDate = TruncateToMilliseconds(DateTime.UtcNow.AddDays(-1));
        var item = new UserKeyModel
        {
            Id = userId,
            Key = "test-key",
            CreationDate = creationDate
        };

        await _repository.CreateAsync(item);
        var result = await _repository.ReadAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("test-key", result.Key);
        Assert.Equal(creationDate, result.CreationDate);
        Assert.Null(result.RevisionDate);
        Assert.Null(result.LastAccessDate);
    }

    [Fact]
    public async Task ReadAsync_ReturnsNull_WhenItemDoesNotExist()
    {
        var result = await _repository.ReadAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var userId = Guid.NewGuid();
        var item = new UserKeyModel { Id = userId, Key = "original-key" };
        await _repository.CreateAsync(item);

        var stored = await _repository.ReadAsync(userId);
        stored.Key = "updated-key";
        var revisionDate = TruncateToMilliseconds(DateTime.UtcNow.AddDays(-1));
        stored.RevisionDate = revisionDate;
        await _repository.UpdateAsync(stored);

        var result = await _repository.ReadAsync(userId);
        Assert.Equal("updated-key", result.Key);
        Assert.Equal(revisionDate, result.RevisionDate);
    }

    [Fact]
    public async Task DeleteAsync_RemovesItem()
    {
        var userId = Guid.NewGuid();
        var item = new UserKeyModel { Id = userId, Key = "test-key" };
        await _repository.CreateAsync(item);

        await _repository.DeleteAsync(userId);

        var result = await _repository.ReadAsync(userId);
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadAllAsync_ReturnsAllItems()
    {
        var item1 = new UserKeyModel { Id = Guid.NewGuid(), Key = "key-1" };
        var item2 = new UserKeyModel { Id = Guid.NewGuid(), Key = "key-2" };
        await _repository.CreateAsync(item1);
        await _repository.CreateAsync(item2);

        var results = await _repository.ReadAllAsync();

        Assert.True(results.Count >= 2);
        Assert.Contains(results, r => r.Id == item1.Id);
        Assert.Contains(results, r => r.Id == item2.Id);
    }

    // MongoDB stores DateTime with millisecond precision, truncating sub-millisecond ticks.
    private static DateTime TruncateToMilliseconds(DateTime dt) =>
        new(dt.Ticks - (dt.Ticks % TimeSpan.TicksPerMillisecond), dt.Kind);
}
