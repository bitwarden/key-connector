using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace KeyConnector.Tests.Helpers;

public static class JwtTestHelper
{
    public const string TestIssuer = "https://identity.test.bitwarden.com";
    public const string AuthenticationScheme = "TestJwtBearer";

    private const string _signingKeyId = "test-signing-key";
    private const string _discoveryPath = "/.well-known/openid-configuration";
    private const string _jwksPath = "/.well-known/openid-configuration/jwks";

    private static readonly RSA _rsa = LoadSigningRsa();
    private static readonly RsaSecurityKey _signingKey = new(_rsa) { KeyId = _signingKeyId };
    private static readonly string _discoveryJson = BuildDiscoveryJson();
    private static readonly string _jwksJson = BuildJwksJson();

    public static string CreateToken(Guid userId)
    {
        return CreateToken(new JwtTokenOptions { Subject = userId.ToString() });
    }

    public static string CreateToken(JwtTokenOptions options)
    {
        var now = DateTime.UtcNow;
        var expires = options.Expires ?? now.AddHours(1);
        // Keep the "not before" and "issued at" times consistent with the expiry so an
        // explicitly expired token fails because it has expired, not because it is not yet valid.
        var issuedAt = expires < now ? expires.AddMinutes(-5) : now;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = issuedAt,
            NotBefore = options.NotBefore ?? issuedAt,
            Expires = expires,
            TokenType = "at+jwt",
            SigningCredentials = options.SigningCredentials
                ?? new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256),
            // Null-valued options are dropped so a claim can be genuinely absent, not present-but-null.
            Claims = new Dictionary<string, object>
            {
                ["sub"] = options.Subject,
                ["email"] = options.Email,
                ["scope"] = options.Scope,
                ["amr"] = options.Amr,
            }.Where(claim => claim.Value is not null).ToDictionary(claim => claim.Key, claim => claim.Value),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public static HttpMessageHandler CreateDiscoveryHandler()
    {
        return new MockOidcHandler();
    }

    private static RSA LoadSigningRsa()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "test-jwt-signing-key.pem");
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(path));
        return rsa;
    }

    private static string BuildDiscoveryJson()
    {
        // Minimal OpenID Connect discovery document (OpenID Connect Discovery 1.0 / RFC 8414
        // "OAuth 2.0 Authorization Server Metadata"). Bearer-token validation only consumes
        // "issuer" (to validate the token's issuer claim) and "jwks_uri" (to fetch the signing keys).
        return JsonSerializer.Serialize(new
        {
            issuer = TestIssuer,
            jwks_uri = TestIssuer + _jwksPath,
        });
    }

    private static string BuildJwksJson()
    {
        var publicKey = new RsaSecurityKey(_rsa.ExportParameters(false)) { KeyId = _signingKeyId };
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(publicKey);
        // JSON Web Key Set (RFC 7517) publishing the signing key's public half for signature verification.
        return JsonSerializer.Serialize(new
        {
            keys = new[] { jwk },
        });
    }

    private sealed class MockOidcHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri?.AbsoluteUri ?? string.Empty;
            string json = null;
            if (url.EndsWith(_jwksPath, StringComparison.Ordinal))
            {
                json = _jwksJson;
            }
            else if (url.EndsWith(_discoveryPath, StringComparison.Ordinal))
            {
                json = _discoveryJson;
            }

            if (json == null)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }
}

public sealed class JwtTokenOptions
{
    public string Subject = Guid.NewGuid().ToString();
    public string Email = "test-user@bitwarden.test";
    public string Issuer = JwtTestHelper.TestIssuer;
    public string Audience;
    public object Scope = new[] { "api", "offline_access" };
    public object Amr = new[] { "Application" };
    public DateTime? NotBefore;
    public DateTime? Expires;
    public SigningCredentials SigningCredentials;
}
