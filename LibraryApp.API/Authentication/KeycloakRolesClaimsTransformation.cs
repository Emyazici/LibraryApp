using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace LibraryApp.API.Authentication;

/// <summary>
/// Keycloak realm rollerini ("realm_access": {"roles":[...]}) düz <see cref="ClaimTypes.Role"/> claim'lerine
/// dönüştürür; böylece [Authorize(Roles = ...)] ve User.IsInRole(...) çalışır.
/// İdempotent: aynı rol iki kez eklenmez (IClaimsTransformation istek başına birden fazla çağrılabilir).
/// </summary>
public sealed class KeycloakRolesClaimsTransformation : IClaimsTransformation
{
    private const string RealmAccessClaimType = "realm_access";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity)
            return Task.FromResult(principal);

        var realmAccess = principal.FindFirst(RealmAccessClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess))
            return Task.FromResult(principal);

        foreach (var role in ParseRoles(realmAccess))
        {
            if (!identity.HasClaim(ClaimTypes.Role, role))
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        return Task.FromResult(principal);
    }

    private static List<string> ParseRoles(string realmAccessJson)
    {
        try
        {
            using var document = JsonDocument.Parse(realmAccessJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("roles", out var roles) ||
                roles.ValueKind != JsonValueKind.Array)
                return [];

            return roles.EnumerateArray()
                .Where(r => r.ValueKind == JsonValueKind.String)
                .Select(r => r.GetString())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r!)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
