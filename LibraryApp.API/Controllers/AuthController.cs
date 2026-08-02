using System.Net.Http.Headers;
using LibraryApp.Application.Commands.Register;
using LibraryApp.Infrastructure.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LibraryApp.API.Controllers;

public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Name, string Surname, string Email, string Password);

[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ApiControllerBase
{
    private readonly ISender _sender;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakOptions _keycloakOptions;

    public AuthController(ISender sender, IHttpClientFactory httpClientFactory, IOptions<KeycloakOptions> keycloakOptions)
    {
        _sender = sender;
        _httpClientFactory = httpClientFactory;
        _keycloakOptions = keycloakOptions.Value;
    }

    [HttpPost("register")]
    public async Task<ActionResult<Guid>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new RegisterCommand(request.Name, request.Surname, request.Email, request.Password), ct);
        return FromResult(result);
    }

    // Uygulama kendi şifre doğrulamasını yapmıyor — Keycloak'un token endpoint'ine dev-convenience proxy.
    // Prod/WebUI senaryosunda bunun yerine Authorization Code flow kullanılmalı (bkz. Keycloak-Gecis-Plani.md Faz 7).
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_keycloakOptions.AdminBaseUrl);

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _keycloakOptions.ClientId,
            ["client_secret"] = _keycloakOptions.ClientSecret,
            ["username"] = request.Username,
            ["password"] = request.Password
        };

        var response = await client.PostAsync(
            $"/realms/{_keycloakOptions.Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(form),
            ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, body);

        return Content(body, "application/json");
    }
}
