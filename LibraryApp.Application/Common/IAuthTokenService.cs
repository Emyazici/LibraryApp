namespace LibraryApp.Application.Common;

// Kimlik sağlayıcıdan (Keycloak) kullanıcı adı/şifre ile access token alır.
public interface IAuthTokenService
{
    Task<Result<AuthTokenDto>> RequestTokenAsync(string username, string password, CancellationToken ct = default);
}

public record AuthTokenDto(string AccessToken, int ExpiresIn, string? RefreshToken, string TokenType);
