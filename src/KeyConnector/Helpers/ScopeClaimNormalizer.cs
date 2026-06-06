using System;
using System.Linq;
using System.Security.Claims;
using Bit.KeyConnector.Models;

namespace Bit.KeyConnector.Helpers;

/// <summary>
/// Splits space-delimited OAuth2 scope claims into individual claims.
/// OAuth2 tokens may carry scopes as a single space-delimited string (e.g. "api offline_access"),
/// but authorization policies use exact-match semantics, so each scope must be its own claim.
/// </summary>
public static class ScopeClaimNormalizer
{
    /// <summary>
    /// Finds all "scope" claims on <paramref name="identity"/> and splits any that contain
    /// spaces into one claim per scope value. Single-value scope claims are left unchanged.
    /// </summary>
    public static void Normalize(ClaimsIdentity identity)
    {
        var scopeClaims = identity.FindAll(JwtClaimTypes.Scope).ToList();
        foreach (var claim in scopeClaims.Where(claim => claim.Value.Contains(' ')))
        {
            identity.RemoveClaim(claim);
            foreach (var scope in claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                identity.AddClaim(new Claim(JwtClaimTypes.Scope, scope));
            }
        }
    }
}
