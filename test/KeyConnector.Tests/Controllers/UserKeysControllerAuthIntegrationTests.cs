using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Bit.KeyConnector.Models;
using Bit.KeyConnector.Repositories;
using Bit.KeyConnector.Services.Crypto;
using KeyConnector.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace KeyConnector.Tests.Controllers;

public class UserKeysControllerAuthIntegrationTests : IClassFixture<KeyConnectorWebApplicationFactory>
{
    private const string _testKey = "dGVzdC1rZXktY29ubmVjdG9yLWtleQo=";

    private readonly HttpClient _client;
    private readonly IUserKeyRepository _userKeyRepository;
    private readonly ICryptoService _cryptoService;

    public UserKeysControllerAuthIntegrationTests(KeyConnectorWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _userKeyRepository = factory.Services.GetRequiredService<IUserKeyRepository>();
        _cryptoService = factory.Services.GetRequiredService<ICryptoService>();
    }

    [Fact]
    public async Task Get_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await SendGetAsync(null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsUnauthorized_WhenAuthorizationSchemeIsNotBearer()
    {
        var response = await SendGetAsync(new AuthenticationHeaderValue("Basic", "dXNlcjpwYXNz"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsUnauthorized_WhenTokenSignedWithDifferentKey()
    {
        using var rsa = RSA.Create(2048);
        var foreignKey = new RsaSecurityKey(rsa) { KeyId = "foreign-key" };
        var token = JwtTestHelper.CreateToken(new JwtTokenOptions
        {
            SigningCredentials = new SigningCredentials(foreignKey, SecurityAlgorithms.RsaSha256),
        });

        var response = await SendGetWithToken(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsUnauthorized_WhenIssuerDoesNotMatch()
    {
        var token = JwtTestHelper.CreateToken(new JwtTokenOptions { Issuer = "https://evil.example.com" });

        var response = await SendGetWithToken(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsUnauthorized_WhenTokenExpired()
    {
        var token = JwtTestHelper.CreateToken(new JwtTokenOptions { Expires = DateTime.UtcNow.AddMinutes(-10) });

        var response = await SendGetWithToken(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsUnauthorized_WhenTokenNotYetValid()
    {
        var token = JwtTestHelper.CreateToken(new JwtTokenOptions { NotBefore = DateTime.UtcNow.AddMinutes(10) });

        var response = await SendGetWithToken(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsUnauthorized_WhenTokenMalformed()
    {
        var response = await SendGetWithToken("not-a-jwt");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsForbidden_WhenScopeMissingApi()
    {
        var token = JwtTestHelper.CreateToken(new JwtTokenOptions { Scope = new[] { "offline_access" } });

        var response = await SendGetWithToken(token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsForbidden_WhenAmrIsNotApplicationOrExternal()
    {
        var token = JwtTestHelper.CreateToken(new JwtTokenOptions { Amr = new[] { "sso" } });

        var response = await SendGetWithToken(token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsForbidden_WhenScopeIsSpaceDelimitedString()
    {
        // Bitwarden Identity (Duende IdentityServer) emits "scope" as a JSON array, e.g. ["api","offline_access"]
        // — historical, non-RFC behavior. The "Application" authorization policy depends on that array
        // form, so a single space-delimited string never matches RequireClaim("scope","api") → 403.
        var token = JwtTestHelper.CreateToken(new JwtTokenOptions { Scope = "api offline_access" });

        var response = await SendGetWithToken(token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsForbidden_WhenScopeClaimAbsent()
    {
        var token = JwtTestHelper.CreateToken(new JwtTokenOptions { Scope = null });

        var response = await SendGetWithToken(token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsForbidden_WhenAmrClaimAbsent()
    {
        var token = JwtTestHelper.CreateToken(new JwtTokenOptions { Amr = null });

        var response = await SendGetWithToken(token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsKey_WhenTokenIsValid()
    {
        var userId = Guid.NewGuid();
        await SeedUserAsync(userId, _testKey);

        var response = await SendGetWithToken(JwtTestHelper.CreateToken(userId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UserKeyResponseModel>(TestContext.Current.CancellationToken);
        Assert.Equal(_testKey, result.Key);
    }

    [Fact]
    public async Task Get_ReturnsKey_WhenAmrIsExternal()
    {
        var userId = Guid.NewGuid();
        await SeedUserAsync(userId, _testKey);

        var token = JwtTestHelper.CreateToken(new JwtTokenOptions
        {
            Subject = userId.ToString(),
            Amr = new[] { "external" },
        });
        var response = await SendGetWithToken(token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UserKeyResponseModel>(TestContext.Current.CancellationToken);
        Assert.Equal(_testKey, result.Key);
    }

    [Fact]
    public async Task Get_ReturnsKey_WhenAudiencePresentButNotValidated()
    {
        var userId = Guid.NewGuid();
        await SeedUserAsync(userId, _testKey);

        var token = JwtTestHelper.CreateToken(new JwtTokenOptions
        {
            Subject = userId.ToString(),
            Audience = "https://other-api.example.com",
        });
        var response = await SendGetWithToken(token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UserKeyResponseModel>(TestContext.Current.CancellationToken);
        Assert.Equal(_testKey, result.Key);
    }

    private async Task SeedUserAsync(Guid userId, string plainKey)
    {
        var encryptedKey = await _cryptoService.AesEncryptToB64Async(plainKey);
        await _userKeyRepository.CreateAsync(new UserKeyModel { Id = userId, Key = encryptedKey });
    }

    private Task<HttpResponseMessage> SendGetWithToken(string token)
    {
        return SendGetAsync(new AuthenticationHeaderValue("Bearer", token));
    }

    private async Task<HttpResponseMessage> SendGetAsync(AuthenticationHeaderValue authorization)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/user-keys");
        request.Headers.Authorization = authorization;
        return await _client.SendAsync(request);
    }
}
