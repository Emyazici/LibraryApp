using FluentAssertions;
using LibraryApp.Application.Commands.AddBook;
using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Repositories;
using Moq;

namespace LibraryApp.Tests.Application;

public class AddBookCommandHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepo = new();
    private readonly Mock<IUnitOfWork>     _uow      = new();

    private AddBookCommandHandler Sut() =>
        new(_bookRepo.Object, _uow.Object);

    private static AddBookCommand ValidCommand() =>
        new(Guid.NewGuid(), "Clean Code", "9780134685991", 29.99m, "TRY", 5);

    // ── Failure yolları ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenIsbnAlreadyExists_ReturnsFailure()
    {
        _bookRepo.Setup(r => r.ExistsByIsbnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var result = await Sut().Handle(ValidCommand(), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenIsbnAlreadyExists_DoesNotCallAddAsync()
    {
        _bookRepo.Setup(r => r.ExistsByIsbnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        await Sut().Handle(ValidCommand(), default);

        _bookRepo.Verify(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessWithNonEmptyBookId()
    {
        SetupHappyPath();

        var result = await Sut().Handle(ValidCommand(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsAddAsync()
    {
        SetupHappyPath();

        await Sut().Handle(ValidCommand(), default);

        _bookRepo.Verify(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsSaveChangesOnce()
    {
        SetupHappyPath();

        await Sut().Handle(ValidCommand(), default);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_DoesNotCallSaveChangesWhenIsbnExists()
    {
        _bookRepo.Setup(r => r.ExistsByIsbnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        await Sut().Handle(ValidCommand(), default);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private void SetupHappyPath()
    {
        _bookRepo.Setup(r => r.ExistsByIsbnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _bookRepo.Setup(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Book b, CancellationToken _) => b);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}
