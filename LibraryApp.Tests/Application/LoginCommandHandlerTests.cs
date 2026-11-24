using FluentAssertions;
using LibraryApp.Application.Commands.Login;
using LibraryApp.Application.Common;
using Moq;

namespace LibraryApp.Tests.Application;

public class LoginCommandHandlerTests
{
    private readonly Mock<IAuthTokenService> _authTokenService = new();

    private LoginCommandHandler Sut() => new(_authTokenService.Object);

    private static LoginCommand ValidCommand() => new("customer@libraryapp.local", "P@ssw0rd!");

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokenFromService()
    {
        var token = new AuthTokenDto("access-token", 300, "refresh-token", "Bearer");
        _authTokenService
            .Setup(s => s.RequestTokenAsync("customer@libraryapp.local", "P@ssw0rd!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(token));

        var result = await Sut().Handle(ValidCommand(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(token);
    }

    // ── Failure yolları ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_InvalidCredentials_ReturnsFailure()
    {
        _authTokenService
            .Setup(s => s.RequestTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AuthTokenDto>("Invalid username or password."));

        var result = await Sut().Handle(ValidCommand(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid username or password.");
    }
}
