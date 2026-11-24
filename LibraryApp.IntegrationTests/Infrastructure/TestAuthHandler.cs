using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LibraryApp.IntegrationTests.Infrastructure;

/// <summary>
/// Replaces Keycloak JWT validation in tests. The caller's identity is taken from request headers:
/// <c>X-Test-UserId</c> becomes the <c>sub</c> claim and <c>X-Test-Roles</c> (comma separated) become
/// <see cref="ClaimTypes.Role"/> claims. Without the user header no identity is produced, so
/// <c>[Authorize]</c> endpoints answer 401 exactly like a request without a bearer token.
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string UserIdHeader = "X-Test-UserId";
    public const string RolesHeader = "X-Test-Roles";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userIdValues) ||
            string.IsNullOrWhiteSpace(userIdValues.ToString()))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = userIdValues.ToString();
        var claims = new List<Claim>
        {
            new("sub", userId),
            new("preferred_username", $"test-{userId}")
        };

        if (Request.Headers.TryGetValue(RolesHeader, out var rolesValues))
        {
            var roles = rolesValues.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        }

        var identity = new ClaimsIdentity(claims, SchemeName, "sub", ClaimTypes.Role);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
