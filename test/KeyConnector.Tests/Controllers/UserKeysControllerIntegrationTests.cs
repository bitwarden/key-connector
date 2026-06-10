using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Bit.KeyConnector.Models;
using Bit.KeyConnector.Repositories;
using Bit.KeyConnector.Services.Crypto;
using KeyConnector.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeyConnector.Tests.Controllers;

public class UserKeysControllerIntegrationTests : IClassFixture<KeyConnectorWebApplicationFactory>
{
    private const string _testKey = "dGVzdC1rZXktY29ubmVjdG9yLWtleQo=";
    private const string _testUpdatedKey = "dGVzdC11cGRhdGVkLWtleS1jb25uZWN0b3Ita2V5Cg==";

    private readonly HttpClient _client;
    private readonly IUserKeyRepository _userKeyRepository;
    private readonly ICryptoService _cryptoService;

    public UserKeysControllerIntegrationTests(KeyConnectorWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        var scope = factory.Services;
        _userKeyRepository = scope.GetRequiredService<IUserKeyRepository>();
        _cryptoService = scope.GetRequiredService<ICryptoService>();
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var request = CreateRequest(HttpMethod.Get, Guid.NewGuid());

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsDecryptedKey_WhenUserExistsInDatabase()
    {
        var userId = Guid.NewGuid();
        var encryptedKey = await _cryptoService.AesEncryptToB64Async(_testKey);
        await _userKeyRepository.CreateAsync(new UserKeyModel { Id = userId, Key = encryptedKey });

        var request = CreateRequest(HttpMethod.Get, userId);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UserKeyResponseModel>(TestContext.Current.CancellationToken);
        Assert.Equal(_testKey, result.Key);
    }

    [Fact]
    public async Task Post_ThenGet_RoundTripsKey()
    {
        var userId = Guid.NewGuid();
        var beforePost = DateTime.UtcNow;

        var postRequest = CreateRequest(HttpMethod.Post, userId, new { Key = _testKey });
        var postResponse = await _client.SendAsync(postRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var getRequest = CreateRequest(HttpMethod.Get, userId);
        var getResponse = await _client.SendAsync(getRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var result = await getResponse.Content.ReadFromJsonAsync<UserKeyResponseModel>(TestContext.Current.CancellationToken);
        Assert.Equal(_testKey, result.Key);

        var stored = await _userKeyRepository.ReadAsync(userId);
        Assert.InRange(stored.CreationDate, beforePost, DateTime.UtcNow);
    }

    [Fact]
    public async Task Post_ReturnsBadRequest_WhenUserAlreadyExists()
    {
        var userId = Guid.NewGuid();

        var postRequest = CreateRequest(HttpMethod.Post, userId, new { Key = _testKey });
        await _client.SendAsync(postRequest, TestContext.Current.CancellationToken);

        var duplicateRequest = CreateRequest(HttpMethod.Post, userId, new { Key = _testKey });
        var response = await _client.SendAsync(duplicateRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_CreatesUser_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var beforePut = DateTime.UtcNow;

        var putRequest = CreateRequest(HttpMethod.Put, userId, new { Key = _testKey });
        var putResponse = await _client.SendAsync(putRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var getRequest = CreateRequest(HttpMethod.Get, userId);
        var getResponse = await _client.SendAsync(getRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var result = await getResponse.Content.ReadFromJsonAsync<UserKeyResponseModel>(TestContext.Current.CancellationToken);
        Assert.Equal(_testKey, result.Key);

        var stored = await _userKeyRepository.ReadAsync(userId);
        Assert.InRange(stored.CreationDate, beforePut, DateTime.UtcNow);
    }

    [Fact]
    public async Task Put_UpdatesKey_ThenGetReturnsUpdatedKey()
    {
        var userId = Guid.NewGuid();

        var postRequest = CreateRequest(HttpMethod.Post, userId, new { Key = _testKey });
        await _client.SendAsync(postRequest, TestContext.Current.CancellationToken);

        var beforePut = DateTime.UtcNow;
        var putRequest = CreateRequest(HttpMethod.Put, userId, new { Key = _testUpdatedKey });
        var putResponse = await _client.SendAsync(putRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var getRequest = CreateRequest(HttpMethod.Get, userId);
        var getResponse = await _client.SendAsync(getRequest, TestContext.Current.CancellationToken);
        var result = await getResponse.Content.ReadFromJsonAsync<UserKeyResponseModel>(TestContext.Current.CancellationToken);
        Assert.Equal(_testUpdatedKey, result.Key);

        var stored = await _userKeyRepository.ReadAsync(userId);
        Assert.NotNull(stored.RevisionDate);
        Assert.InRange(stored.RevisionDate.Value, beforePut, DateTime.UtcNow);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, Guid userId, object body = null)
    {
        var request = new HttpRequestMessage(method, "/user-keys");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken(userId));
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }
        return request;
    }
}
