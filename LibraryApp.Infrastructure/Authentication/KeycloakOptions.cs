namespace LibraryApp.Infrastructure.Authentication;

public class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string Authority { get; set; } = string.Empty; // ör. http://localhost:8180/realms/libraryapp
    public string Realm { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty; // ör. account
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AdminBaseUrl { get; set; } = string.Empty; // ör. http://localhost:8180

    // Opsiyonel: API, Keycloak'a token'daki issuer'dan farklı bir adresle (ör. Docker ağı içinden
    // http://keycloak:8080) erişiyorsa OIDC metadata bu adresten çekilir.
    public string? MetadataAddress { get; set; }

    // Opsiyonel: Token'daki "iss" değeri Authority'den farklıysa (ör. http://localhost:8180/realms/libraryapp).
    public string? ValidIssuer { get; set; }
}
