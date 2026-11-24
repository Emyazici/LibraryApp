using FluentAssertions;
using LibraryApp.Application.Commands.Register;
using LibraryApp.Application.Common;
using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Repositories;
using Moq;

namespace LibraryApp.Tests.Application;

public class RegisterCommandHandlerTests
{
    private const string KeycloakUserId = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";

    private readonly Mock<IMemberRepository>    _memberRepo = new();
    private readonly Mock<IUnitOfWork>          _uow        = new();
    private readonly Mock<IKeycloakAdminClient> _keycloak   = new();

    private RegisterCommandHandler Sut() =>
        new(_memberRepo.Object, _uow.Object, _keycloak.Object);

    private static RegisterCommand ValidCommand() =>
        new("Ada", "Lovelace", "ada@libraryapp.local", "P@ssw0rd!");

    // ── Failure yolları ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ReturnsFailureAndDoesNotCallKeycloak()
    {
        _memberRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true);

        var result = await Sut().Handle(ValidCommand(), default);

        result.IsFailure.Should().BeTrue();
        _keycloak.Verify(k => k.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _memberRepo.Verify(r => r.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenKeycloakFails_ReturnsFailureAndDoesNotAddMember()
    {
        _memberRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(false);
        SetupKeycloak(Result.Failure<string>("Keycloak unavailable"));

        var result = await Sut().Handle(ValidCommand(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Keycloak unavailable");
        _memberRepo.Verify(r => r.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_AddsMemberWithKeycloakIdAndSavesOnce()
    {
        _memberRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(false);
        _memberRepo.Setup(r => r.AddAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Member m, CancellationToken _) => m);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        SetupKeycloak(Result.Success(KeycloakUserId));

        var result = await Sut().Handle(ValidCommand(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Guid.Parse(KeycloakUserId));
        _memberRepo.Verify(r => r.AddAsync(
            It.Is<Member>(m => m.Id == Guid.Parse(KeycloakUserId) && m.Email == "ada@libraryapp.local"),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private void SetupKeycloak(Result<string> result) =>
        _keycloak.Setup(k => k.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                     It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(result);
}
