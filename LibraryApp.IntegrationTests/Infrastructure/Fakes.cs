using LibraryApp.Application.Common;

namespace LibraryApp.IntegrationTests.Infrastructure;

/// <summary>Stands in for Keycloak's admin API: every user gets a fresh "sub".</summary>
public sealed class FakeKeycloakAdminClient : IKeycloakAdminClient
{
    public const string FailingEmail = "keycloak-fail@test.local";

    public Task<Result<string>> CreateUserAsync(
        string username,
        string email,
        string firstName,
        string lastName,
        string password,
        string realmRole,
        CancellationToken ct = default)
    {
        if (string.Equals(email, FailingEmail, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(Result.Failure<string>("Keycloak kullanıcı oluşturamadı."));

        return Task.FromResult(Result.Success(Guid.NewGuid().ToString()));
    }
}

/// <summary>Stands in for Keycloak's token endpoint (password grant).</summary>
public sealed class FakeAuthTokenService : IAuthTokenService
{
    public const string ValidUsername = "testuser@test.local";
    public const string ValidPassword = "Test123!";
    public const string AccessToken = "fake-access-token";
    public const string InvalidCredentialsMessage = "Kullanıcı adı veya şifre hatalı.";

    public Task<Result<AuthTokenDto>> RequestTokenAsync(string username, string password, CancellationToken ct = default)
    {
        if (username == ValidUsername && password == ValidPassword)
        {
            return Task.FromResult(Result.Success(
                new AuthTokenDto(AccessToken, 300, "fake-refresh-token", "Bearer")));
        }

        return Task.FromResult(Result.Failure<AuthTokenDto>(InvalidCredentialsMessage));
    }
}
