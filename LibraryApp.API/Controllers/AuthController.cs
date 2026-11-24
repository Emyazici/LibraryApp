using LibraryApp.API.Extensions;
using LibraryApp.Application.Commands.Login;
using LibraryApp.Application.Commands.Register;
using LibraryApp.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApp.API.Controllers;

public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Name, string Surname, string Email, string Password);

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(ISender sender) : ControllerBase
{
    /// <summary>Yeni müşteri kaydı oluşturur (Keycloak kullanıcısı + Member). Oluşan üyenin id'sini döner.</summary>
    [HttpPost("register")]
    [ProducesResponseType<Guid>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RegisterCommand(request.Name, request.Surname, request.Email, request.Password), ct);
        return result.ToActionResult();
    }

    /// <summary>Kullanıcı adı/şifre ile Keycloak'tan access token alır (dev/demo kolaylığı).</summary>
    [HttpPost("login")]
    [ProducesResponseType<AuthTokenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthTokenDto>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new LoginCommand(request.Username, request.Password), ct);
        return result.ToActionResult();
    }
}
