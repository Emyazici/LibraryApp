using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LibraryApp.Application.Common;
using Microsoft.Extensions.Options;

namespace LibraryApp.Infrastructure.Authentication;

public class KeycloakAdminClient : IKeycloakAdminClient
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakOptions _options;

    public KeycloakAdminClient(HttpClient httpClient, IOptions<KeycloakOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<Result<string>> CreateUserAsync(
        string username,
        string email,
        string firstName,
        string lastName,
        string password,
        string realmRole,
        CancellationToken ct = default)
    {
        var adminTokenResult = await GetAdminAccessTokenAsync(ct);
        if (adminTokenResult.IsFailure)
            return Result.Failure<string>(adminTokenResult.Error);

        var adminToken = adminTokenResult.Value;

        // Keycloak varsayılan "User Profile" özelliği email/firstName/lastName alanlarını zorunlu tutuyor,
        // eksikse Resource Owner Password flow "Account is not fully set up" hatası veriyor.
        var createUserRequest = new HttpRequestMessage(HttpMethod.Post, $"/admin/realms/{_options.Realm}/users")
        {
            Content = JsonContent.Create(new
            {
                username,
                email,
                firstName,
                lastName,
                enabled = true,
                emailVerified = true,
                requiredActions = Array.Empty<string>(),
                credentials = new[]
                {
                    new { type = "password", value = password, temporary = false }
                }
            })
        };
        createUserRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createUserResponse = await _httpClient.SendAsync(createUserRequest, ct);
        if (!createUserResponse.IsSuccessStatusCode)
        {
            var body = await createUserResponse.Content.ReadAsStringAsync(ct);
            if (createUserResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
                return Result.Failure<string>("Bu kullanıcı adı veya e-posta Keycloak'ta zaten kayıtlı.");

            return Result.Failure<string>($"Keycloak kullanıcı oluşturma başarısız: {createUserResponse.StatusCode} {body}");
        }

        var location = createUserResponse.Headers.Location?.ToString();
        var keycloakUserId = location?.Split('/').LastOrDefault();
        if (string.IsNullOrWhiteSpace(keycloakUserId))
            return Result.Failure<string>("Keycloak kullanıcı id'si alınamadı.");

        var assignRoleResult = await AssignRealmRoleAsync(adminToken, keycloakUserId, realmRole, ct);
        if (assignRoleResult.IsFailure)
            return Result.Failure<string>(assignRoleResult.Error);

        return Result.Success(keycloakUserId);
    }

    private async Task<Result<string>> GetAdminAccessTokenAsync(CancellationToken ct)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret
        };

        var response = await _httpClient.PostAsync(
            $"/realms/{_options.Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(form),
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            return Result.Failure<string>($"Keycloak admin token alınamadı: {response.StatusCode} {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
        if (payload?.AccessToken is null)
            return Result.Failure<string>("Keycloak admin token yanıtı boş.");

        return Result.Success(payload.AccessToken);
    }

    private async Task<Result> AssignRealmRoleAsync(string adminToken, string keycloakUserId, string realmRole, CancellationToken ct)
    {
        var getRoleRequest = new HttpRequestMessage(HttpMethod.Get, $"/admin/realms/{_options.Realm}/roles/{realmRole}");
        getRoleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var getRoleResponse = await _httpClient.SendAsync(getRoleRequest, ct);
        if (!getRoleResponse.IsSuccessStatusCode)
            return Result.Failure($"Realm rolü '{realmRole}' bulunamadı.");

        var role = await getRoleResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        var assignRequest = new HttpRequestMessage(HttpMethod.Post, $"/admin/realms/{_options.Realm}/users/{keycloakUserId}/role-mappings/realm")
        {
            Content = JsonContent.Create(new[] { role })
        };
        assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var assignResponse = await _httpClient.SendAsync(assignRequest, ct);
        if (!assignResponse.IsSuccessStatusCode)
        {
            var body = await assignResponse.Content.ReadAsStringAsync(ct);
            return Result.Failure($"Realm rolü atanamadı: {assignResponse.StatusCode} {body}");
        }

        return Result.Success();
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}
