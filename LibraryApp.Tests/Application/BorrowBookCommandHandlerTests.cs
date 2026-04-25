using FluentAssertions;
using LibraryApp.Application.Commands.BorrowBook;
using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Repositories;
using Moq;

namespace LibraryApp.Tests.Application;

public class BorrowBookCommandHandlerTests
{
    private readonly Mock<IBookRepository>   _bookRepo   = new();
    private readonly Mock<IMemberRepository> _memberRepo = new();
    private readonly Mock<ILoanRepository>   _loanRepo   = new();
    private readonly Mock<IUnitOfWork>       _uow        = new();

    private BorrowBookCommandHandler Sut() =>
        new(_bookRepo.Object, _memberRepo.Object, _loanRepo.Object, _uow.Object);

    private static Book ValidBook()
        => Book.Create(Guid.NewGuid(), "Clean Code", "9780134685991", 10m, "TRY", 5);

    private static BorrowBookCommand ValidCommand(Guid bookId, Guid memberId) =>
        new(bookId, memberId, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddDays(14));

    // ── Failure yolları ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenBookNotFound_ReturnsFailure()
    {
        _bookRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Book?)null);

        var result = await Sut().Handle(ValidCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenMemberNotFound_ReturnsFailure()
    {
        var book = ValidBook();
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(book);
        _memberRepo.Setup(r => r.ExistsByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(false);

        var result = await Sut().Handle(ValidCommand(book.Id, Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenAlreadyHasActiveLoan_ReturnsFailure()
    {
        var book     = ValidBook();
        var memberId = Guid.NewGuid();
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(book);
        _memberRepo.Setup(r => r.ExistsByIdAsync(memberId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true);
        _loanRepo.Setup(r => r.HasActiveLoanAsync(memberId, book.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var result = await Sut().Handle(ValidCommand(book.Id, memberId), default);

        result.IsFailure.Should().BeTrue();
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessWithNonEmptyLoanId()
    {
        var book     = ValidBook();
        var memberId = Guid.NewGuid();
        SetupHappyPath(book, memberId);

        var result = await Sut().Handle(ValidCommand(book.Id, memberId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsBookUpdateAsync()
    {
        var book     = ValidBook();
        var memberId = Guid.NewGuid();
        SetupHappyPath(book, memberId);

        await Sut().Handle(ValidCommand(book.Id, memberId), default);

        _bookRepo.Verify(r => r.UpdateAsync(book, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsLoanAddAsync()
    {
        var book     = ValidBook();
        var memberId = Guid.NewGuid();
        SetupHappyPath(book, memberId);

        await Sut().Handle(ValidCommand(book.Id, memberId), default);

        _loanRepo.Verify(r => r.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsSaveChangesOnce()
    {
        var book     = ValidBook();
        var memberId = Guid.NewGuid();
        SetupHappyPath(book, memberId);

        await Sut().Handle(ValidCommand(book.Id, memberId), default);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_BookStockDecreased()
    {
        var book     = ValidBook();
        var memberId = Guid.NewGuid();
        var stockBefore = book.TotalStock;
        SetupHappyPath(book, memberId);

        await Sut().Handle(ValidCommand(book.Id, memberId), default);

        book.TotalStock.Should().Be(stockBefore - 1);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private void SetupHappyPath(Book book, Guid memberId)
    {
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(book);
        _memberRepo.Setup(r => r.ExistsByIdAsync(memberId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true);
        _loanRepo.Setup(r => r.HasActiveLoanAsync(memberId, book.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _loanRepo.Setup(r => r.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Loan l, CancellationToken _) => l);
        _bookRepo.Setup(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}
