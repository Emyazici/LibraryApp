using LibraryApp.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LibraryApp.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Keycloak token'ında kullanıcı id'si "sub" claim'inde gelir.
    public Guid UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return userIdClaim is not null && Guid.TryParse(userIdClaim, out var userId)
                ? userId
                : Guid.Empty;
        }
    }

    // Keycloak "preferred_username" claim'ini kullanır.
    public string UserName =>
        _httpContextAccessor.HttpContext?.User
            .FindFirst("preferred_username")?.Value ?? string.Empty;

    // realm_access.roles içindeki roller Program.cs'teki ClaimsTransformation ile ClaimTypes.Role'e taşınıyor, IsInRole değişmeden çalışır.
    public bool IsAdmin =>
        _httpContextAccessor.HttpContext?.User
            .IsInRole("Admin") ?? false;
}
