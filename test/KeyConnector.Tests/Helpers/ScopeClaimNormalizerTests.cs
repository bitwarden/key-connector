using System.Linq;
using System.Security.Claims;
using Bit.KeyConnector.Helpers;
using Bit.KeyConnector.Models;
using Xunit;

namespace KeyConnector.Tests.Helpers;

public class ScopeClaimNormalizerTests
{
    [Fact]
    public void NormalizeScopeClaims_SplitsSpaceDelimitedScope()
    {
        var identity = new ClaimsIdentity([new Claim(JwtClaimTypes.Scope, "api offline_access")]);

        ScopeClaimNormalizer.Normalize(identity);

        var scopes = identity.FindAll(JwtClaimTypes.Scope).Select(c => c.Value).ToList();
        Assert.Equal(2, scopes.Count);
        Assert.Contains("api", scopes);
        Assert.Contains("offline_access", scopes);
    }

    [Fact]
    public void NormalizeScopeClaims_LeavesSingleScopeUnchanged()
    {
        var identity = new ClaimsIdentity([new Claim(JwtClaimTypes.Scope, "api")]);

        ScopeClaimNormalizer.Normalize(identity);

        var scopeClaim = Assert.Single(identity.FindAll(JwtClaimTypes.Scope));
        Assert.Equal("api", scopeClaim.Value);
    }

    [Fact]
    public void NormalizeScopeClaims_HandlesMultipleScopeClaims()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(JwtClaimTypes.Scope, "api offline_access"),
            new Claim(JwtClaimTypes.Scope, "openid"),
        ]);

        ScopeClaimNormalizer.Normalize(identity);

        var scopes = identity.FindAll(JwtClaimTypes.Scope).Select(c => c.Value).ToList();
        Assert.Equal(3, scopes.Count);
        Assert.Contains("api", scopes);
        Assert.Contains("offline_access", scopes);
        Assert.Contains("openid", scopes);
    }

    [Fact]
    public void NormalizeScopeClaims_IgnoresExtraSpaces()
    {
        var identity = new ClaimsIdentity([new Claim(JwtClaimTypes.Scope, "api  offline_access")]);

        ScopeClaimNormalizer.Normalize(identity);

        var scopes = identity.FindAll(JwtClaimTypes.Scope).Select(c => c.Value).ToList();
        Assert.Equal(2, scopes.Count);
        Assert.Contains("api", scopes);
        Assert.Contains("offline_access", scopes);
    }

    [Fact]
    public void NormalizeScopeClaims_NoScopeClaims_DoesNothing()
    {
        var identity = new ClaimsIdentity([new Claim(JwtClaimTypes.Subject, "user-123")]);

        ScopeClaimNormalizer.Normalize(identity);

        Assert.Empty(identity.FindAll(JwtClaimTypes.Scope));
        var subClaim = Assert.Single(identity.FindAll(JwtClaimTypes.Subject));
        Assert.Equal("user-123", subClaim.Value);
    }

    [Fact]
    public void NormalizeScopeClaims_DoesNotAffectOtherClaims()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(JwtClaimTypes.Subject, "user-123"),
            new Claim(JwtClaimTypes.Email, "test@example.com"),
            new Claim(JwtClaimTypes.Scope, "api offline_access"),
        ]);

        ScopeClaimNormalizer.Normalize(identity);

        Assert.Equal("user-123", identity.FindFirst(JwtClaimTypes.Subject)?.Value);
        Assert.Equal("test@example.com", identity.FindFirst(JwtClaimTypes.Email)?.Value);
    }
}
