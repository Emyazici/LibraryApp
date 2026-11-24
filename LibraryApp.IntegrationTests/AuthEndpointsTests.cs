using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryApp.Application.Common;
using LibraryApp.IntegrationTests.Infrastructure;

namespace LibraryApp.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class AuthEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var client = _factory.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = FakeAuthTokenService.ValidUsername,
            password = FakeAuthTokenService.ValidPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = await response.Content.ReadFromJsonAsync<AuthTokenDto>();
        token.Should().NotBeNull();
        token!.AccessToken.Should().Be(FakeAuthTokenService.AccessToken);
        token.TokenType.Should().Be("Bearer");
        token.ExpiresIn.Should().BePositive();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns400ProblemDetails()
    {
        var client = _factory.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = FakeAuthTokenService.ValidUsername,
            password = "wrong-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.TryGetPropertyIgnoreCase("detail", out var detail).Should().BeTrue();
        detail.GetString().Should().Be(FakeAuthTokenService.InvalidCredentialsMessage);
    }

    [Fact]
    public async Task Register_WithInvalidEmailAndEmptyPassword_Returns400WithValidationErrors()
    {
        var client = _factory.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Ada",
            surname = "Lovelace",
            email = "not-an-email",
            password = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();

        problem.TryGetPropertyIgnoreCase("errors", out var errors).Should().BeTrue("ValidationProblemDetails carries an errors dictionary");
        var errorKeys = errors.EnumerateObject().Select(p => p.Name).ToList();
        errorKeys.Should().Contain(k => string.Equals(k, "Email", StringComparison.OrdinalIgnoreCase));
        errorKeys.Should().Contain(k => string.Equals(k, "Password", StringComparison.OrdinalIgnoreCase));
    }
}
