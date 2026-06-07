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

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsDecryptedKey_WhenUserExistsInDatabase()
    {
        var userId = Guid.NewGuid();
        var plainKey = GenerateBase64Key("direct-insert-key");
        var encryptedKey = await _cryptoService.AesEncryptToB64Async(plainKey);
        await _userKeyRepository.CreateAsync(new UserKeyModel { Id = userId, Key = encryptedKey });

        var request = CreateRequest(HttpMethod.Get, userId);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UserKeyResponseModel>();
        Assert.Equal(plainKey, result.Key);
    }

    [Fact]
    public async Task Post_ThenGet_RoundTripsKey()
    {
        var userId = Guid.NewGuid();
        var key = GenerateBase64Key("test-key-value");
        var beforePost = DateTime.UtcNow;

        var postRequest = CreateRequest(HttpMethod.Post, userId, new { Key = key });
        var postResponse = await _client.SendAsync(postRequest);
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var getRequest = CreateRequest(HttpMethod.Get, userId);
        var getResponse = await _client.SendAsync(getRequest);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var result = await getResponse.Content.ReadFromJsonAsync<UserKeyResponseModel>();
        Assert.Equal(key, result.Key);

        var stored = await _userKeyRepository.ReadAsync(userId);
        Assert.InRange(stored.CreationDate, beforePost, DateTime.UtcNow);
    }

    [Fact]
    public async Task Post_ReturnsBadRequest_WhenUserAlreadyExists()
    {
        var userId = Guid.NewGuid();

        var postRequest = CreateRequest(HttpMethod.Post, userId, new { Key = GenerateBase64Key("key1") });
        await _client.SendAsync(postRequest);

        var duplicateRequest = CreateRequest(HttpMethod.Post, userId, new { Key = GenerateBase64Key("key2") });
        var response = await _client.SendAsync(duplicateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_CreatesUser_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var key = GenerateBase64Key("put-create-key");
        var beforePut = DateTime.UtcNow;

        var putRequest = CreateRequest(HttpMethod.Put, userId, new { Key = key });
        var putResponse = await _client.SendAsync(putRequest);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var getRequest = CreateRequest(HttpMethod.Get, userId);
        var getResponse = await _client.SendAsync(getRequest);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var result = await getResponse.Content.ReadFromJsonAsync<UserKeyResponseModel>();
        Assert.Equal(key, result.Key);

        var stored = await _userKeyRepository.ReadAsync(userId);
        Assert.InRange(stored.CreationDate, beforePut, DateTime.UtcNow);
    }

    [Fact]
    public async Task Put_UpdatesKey_ThenGetReturnsUpdatedKey()
    {
        var userId = Guid.NewGuid();

        var originalKey = GenerateBase64Key("original-key");
        var updatedKey = GenerateBase64Key("updated-key");

        var postRequest = CreateRequest(HttpMethod.Post, userId, new { Key = originalKey });
        await _client.SendAsync(postRequest);

        var beforePut = DateTime.UtcNow;
        var putRequest = CreateRequest(HttpMethod.Put, userId, new { Key = updatedKey });
        var putResponse = await _client.SendAsync(putRequest);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var getRequest = CreateRequest(HttpMethod.Get, userId);
        var getResponse = await _client.SendAsync(getRequest);
        var result = await getResponse.Content.ReadFromJsonAsync<UserKeyResponseModel>();
        Assert.Equal(updatedKey, result.Key);

        var stored = await _userKeyRepository.ReadAsync(userId);
        Assert.NotNull(stored.RevisionDate);
        Assert.InRange(stored.RevisionDate.Value, beforePut, DateTime.UtcNow);
    }

    private static string GenerateBase64Key(string label)
    {
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(label));
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
