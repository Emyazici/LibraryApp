namespace LibraryApp.Application.Common;

public interface IKeycloakAdminClient
{
    // Keycloak'ta yeni kullanıcı oluşturur ve realm role'ünü atar. Başarılıysa Keycloak'ın ürettiği "sub" (kullanıcı id) değerini döner.
    Task<Result<string>> CreateUserAsync(
        string username,
        string email,
        string firstName,
        string lastName,
        string password,
        string realmRole,
        CancellationToken ct = default);
}
