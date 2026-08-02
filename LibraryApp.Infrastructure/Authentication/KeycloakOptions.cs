namespace LibraryApp.Infrastructure.Authentication;

public class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string Authority { get; set; } = string.Empty; // ör. http://localhost:8080/realms/libraryapp
    public string Realm { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty; // ör. libraryapp-api
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AdminBaseUrl { get; set; } = string.Empty; // ör. http://localhost:8080
}
