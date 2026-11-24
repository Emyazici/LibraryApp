using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LibraryApp.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LibraryApp.Infrastructure.Authentication;

// Keycloak token endpoint'ine Resource Owner Password grant ile istek atar (dev/demo kolaylığı).
// Prod/WebUI senaryosunda Authorization Code + PKCE akışı tercih edilmeli.
public class KeycloakAuthTokenService(
    HttpClient httpClient,
    IOptions<KeycloakOptions> options,
    ILogger<KeycloakAuthTokenService> logger) : IAuthTokenService
{
    private const string InvalidCredentialsMessage = "Invalid username or password.";
    private readonly KeycloakOptions _options = options.Value;

    public async Task<Result<AuthTokenDto>> RequestTokenAsync(string username, string password, CancellationToken ct = default)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["username"] = username,
            ["password"] = password
        };

        using var response = await httpClient.PostAsync(
            $"/realms/{_options.Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(form),
            ct);

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
            return Result.Failure<AuthTokenDto>(InvalidCredentialsMessage);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Keycloak token request failed with {StatusCode}: {Body}", response.StatusCode, body);
            return Result.Failure<AuthTokenDto>("Authentication service is unavailable.");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
        if (string.IsNullOrWhiteSpace(payload?.AccessToken))
            return Result.Failure<AuthTokenDto>("Authentication service returned an empty token.");

        return Result.Success(new AuthTokenDto(
            payload.AccessToken,
            payload.ExpiresIn,
            payload.RefreshToken,
            payload.TokenType ?? "Bearer"));
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }
}
