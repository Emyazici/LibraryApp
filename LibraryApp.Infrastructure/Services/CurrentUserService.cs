using System.Security.Claims;
using LibraryApp.Application.Common;
using Microsoft.AspNetCore.Http;

namespace LibraryApp.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return userIdClaim is not null && Guid.TryParse(userIdClaim, out var userId)
                ? userId
                : Guid.Empty;
        }
    }

    public string UserName =>
        _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

    public bool IsAdmin =>
        _httpContextAccessor.HttpContext?.User
            .IsInRole("Admin") ?? false;
}