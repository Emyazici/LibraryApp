using FluentAssertions;
using LibraryApp.Application.Commands.ReturnBook;
using LibraryApp.Application.Common;
using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Repositories;
using LibraryApp.Domain.ValueObjects;
using Moq;

namespace LibraryApp.Tests.Application;

public class ReturnBookCommandHandlerTests
{
    private readonly Mock<ILoanRepository>     _loanRepo    = new();
    private readonly Mock<IBookRepository>     _bookRepo    = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IUnitOfWork>         _uow         = new();

    private ReturnBookCommandHandler Sut() =>
        new(_loanRepo.Object, _bookRepo.Object, _currentUser.Object, _uow.Object);

    private static (Loan loan, Book book) CreateBorrowedPair()
    {
        var book     = Book.Create(Guid.NewGuid(), "Clean Code", "9780134685991", 10m, "TRY", 5);
        book.Borrow();
        var memberId = Guid.NewGuid();
        var period   = LoanPeriod.Create(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddDays(14));
        var loan     = Loan.Create(book.Id, memberId, period);
        return (loan, book);
    }

    // ── Failure yolları ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenLoanNotFound_ReturnsFailure()
    {
        _loanRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Loan?)null);

        var result = await Sut().Handle(new ReturnBookCommand(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenNotOwner_ReturnsFailure()
    {
        var (loan, _) = CreateBorrowedPair();
        _loanRepo.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(loan);
        _currentUser.Setup(u => u.UserId).Returns(Guid.NewGuid()); // farklı kullanıcı

        var result = await Sut().Handle(new ReturnBookCommand(loan.Id), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenBookNotFound_ReturnsFailure()
    {
        var (loan, _) = CreateBorrowedPair();
        _loanRepo.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(loan);
        _currentUser.Setup(u => u.UserId).Returns(loan.MemberId);
        _bookRepo.Setup(r => r.GetByIdAsync(loan.BookId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Book?)null);

        var result = await Sut().Handle(new ReturnBookCommand(loan.Id), default);

        result.IsFailure.Should().BeTrue();
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccess()
    {
        var (loan, book) = CreateBorrowedPair();
        SetupHappyPath(loan, book);

        var result = await Sut().Handle(new ReturnBookCommand(loan.Id), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsLoanUpdateAsync()
    {
        var (loan, book) = CreateBorrowedPair();
        SetupHappyPath(loan, book);

        await Sut().Handle(new ReturnBookCommand(loan.Id), default);

        _loanRepo.Verify(r => r.UpdateAsync(loan, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsBookUpdateAsync()
    {
        var (loan, book) = CreateBorrowedPair();
        SetupHappyPath(loan, book);

        await Sut().Handle(new ReturnBookCommand(loan.Id), default);

        _bookRepo.Verify(r => r.UpdateAsync(book, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsSaveChangesOnce()
    {
        var (loan, book) = CreateBorrowedPair();
        SetupHappyPath(loan, book);

        await Sut().Handle(new ReturnBookCommand(loan.Id), default);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private void SetupHappyPath(Loan loan, Book book)
    {
        _loanRepo.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(loan);
        _currentUser.Setup(u => u.UserId).Returns(loan.MemberId);
        _bookRepo.Setup(r => r.GetByIdAsync(loan.BookId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(book);
        _loanRepo.Setup(r => r.UpdateAsync(loan, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _bookRepo.Setup(r => r.UpdateAsync(book, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}
