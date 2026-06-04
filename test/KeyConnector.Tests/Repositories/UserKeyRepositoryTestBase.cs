using System;
using System.Threading.Tasks;
using Bit.KeyConnector.Models;
using Bit.KeyConnector.Repositories;
using Xunit;

namespace KeyConnector.Tests.Repositories;

public static class ContainerImages
{
    public const string SqlServer = "mcr.microsoft.com/mssql/server:2022-latest";
    public const string PostgreSql = "postgres:14";
    public const string MySql = "mysql:8";
    public const string MariaDb = "mariadb:10";
    public const string Mongo = "mongo:7";
}

public interface IUserKeyRepositoryFixture : IAsyncLifetime
{
    IUserKeyRepository Repository { get; }
}

public abstract class UserKeyRepositoryTestBase<TFixture> : IClassFixture<TFixture>
    where TFixture : class, IUserKeyRepositoryFixture
{
    protected readonly IUserKeyRepository Repository;

    protected UserKeyRepositoryTestBase(TFixture fixture)
    {
        Repository = fixture.Repository;
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

        await Repository.CreateAsync(item);
        var result = await Repository.ReadAsync(userId);

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
        var result = await Repository.ReadAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var userId = Guid.NewGuid();
        var item = new UserKeyModel { Id = userId, Key = "original-key" };
        await Repository.CreateAsync(item);

        var stored = await Repository.ReadAsync(userId);
        stored.Key = "updated-key";
        var revisionDate = TruncateToMilliseconds(DateTime.UtcNow.AddDays(-1));
        stored.RevisionDate = revisionDate;
        await Repository.UpdateAsync(stored);

        var result = await Repository.ReadAsync(userId);
        Assert.Equal("updated-key", result.Key);
        Assert.Equal(revisionDate, result.RevisionDate);
    }

    [Fact]
    public async Task DeleteAsync_RemovesItem()
    {
        var userId = Guid.NewGuid();
        var item = new UserKeyModel { Id = userId, Key = "test-key" };
        await Repository.CreateAsync(item);

        await Repository.DeleteAsync(userId);

        var result = await Repository.ReadAsync(userId);
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadAllAsync_ReturnsAllItems()
    {
        var item1 = new UserKeyModel { Id = Guid.NewGuid(), Key = "key-1" };
        var item2 = new UserKeyModel { Id = Guid.NewGuid(), Key = "key-2" };
        await Repository.CreateAsync(item1);
        await Repository.CreateAsync(item2);

        var results = await Repository.ReadAllAsync();

        Assert.True(results.Count >= 2);
        Assert.Contains(results, r => r.Id == item1.Id);
        Assert.Contains(results, r => r.Id == item2.Id);
    }

    // MongoDB stores DateTime with millisecond precision, truncating sub-millisecond ticks.
    private static DateTime TruncateToMilliseconds(DateTime dt) =>
        new(dt.Ticks - (dt.Ticks % TimeSpan.TicksPerMillisecond), dt.Kind);
}
