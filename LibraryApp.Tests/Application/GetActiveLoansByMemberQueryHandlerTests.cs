using FluentAssertions;
using LibraryApp.Application.Common;
using LibraryApp.Application.Queries.GetActiveLoansByMember;
using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Repositories;
using LibraryApp.Domain.ValueObjects;
using Moq;

namespace LibraryApp.Tests.Application;

public class GetActiveLoansByMemberQueryHandlerTests
{
    private readonly Mock<ILoanRepository>     _loanRepo    = new();
    private readonly Mock<IBookRepository>     _bookRepo    = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private GetActiveLoansByMemberQueryHandler Sut() =>
        new(_loanRepo.Object, _bookRepo.Object, _currentUser.Object);

    private static (Loan loan, Book book) CreateActivePair(Guid memberId)
    {
        var book   = Book.Create(Guid.NewGuid(), "Clean Code", "9780134685991", 10m, "TRY", 5);
        var period = LoanPeriod.Create(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddDays(14));
        var loan   = Loan.Create(book.Id, memberId, period);
        return (loan, book);
    }

    // ── Sonuç içeriği ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoLoans_ReturnsEmptyList()
    {
        var memberId = Guid.NewGuid();
        _currentUser.Setup(u => u.UserId).Returns(memberId);
        _loanRepo.Setup(r => r.GetActiveLoansByMemberAsync(memberId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Loan>());
        _bookRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Book>());

        var result = await Sut().Handle(new GetActiveLoansByMemberQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithLoans_ReturnsDtoPerLoan()
    {
        var memberId        = Guid.NewGuid();
        var (loan1, book1)  = CreateActivePair(memberId);
        var (loan2, book2)  = CreateActivePair(memberId);
        SetupHappyPath(memberId, new[] { loan1, loan2 }, new[] { book1, book2 });

        var result = await Sut().Handle(new GetActiveLoansByMemberQuery(), default);

        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithLoans_DtoContainsCorrectBookTitle()
    {
        var memberId       = Guid.NewGuid();
        var (loan, book)   = CreateActivePair(memberId);
        SetupHappyPath(memberId, new[] { loan }, new[] { book });

        var result = await Sut().Handle(new GetActiveLoansByMemberQuery(), default);

        result.Value.Single().BookTitle.Should().Be(book.Title);
    }

    [Fact]
    public async Task Handle_WithLoans_DtoContainsCorrectLoanId()
    {
        var memberId      = Guid.NewGuid();
        var (loan, book)  = CreateActivePair(memberId);
        SetupHappyPath(memberId, new[] { loan }, new[] { book });

        var result = await Sut().Handle(new GetActiveLoansByMemberQuery(), default);

        result.Value.Single().Id.Should().Be(loan.Id);
    }

    [Fact]
    public async Task Handle_WhenBookMissing_DtoShowsBilinmiyor()
    {
        var memberId = Guid.NewGuid();
        var (loan, _) = CreateActivePair(memberId);
        _currentUser.Setup(u => u.UserId).Returns(memberId);
        _loanRepo.Setup(r => r.GetActiveLoansByMemberAsync(memberId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Loan> { loan });
        _bookRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Book>()); // kitap bulunamadı

        var result = await Sut().Handle(new GetActiveLoansByMemberQuery(), default);

        result.Value.Single().BookTitle.Should().Be("Bilinmiyor");
    }

    // ── N+1 olmaması ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithManyLoans_CallsGetByIdsAsyncOnce()
    {
        var memberId = Guid.NewGuid();
        var pairs    = Enumerable.Range(0, 5).Select(_ => CreateActivePair(memberId)).ToList();
        SetupHappyPath(memberId, pairs.Select(p => p.loan), pairs.Select(p => p.book));

        await Sut().Handle(new GetActiveLoansByMemberQuery(), default);

        // N+1 olsaydı 5 kez çağrılırdı — sadece 1 kez çağrılmalı
        _bookRepo.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(),
                                               It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithManyLoans_CallsGetActiveLoansByMemberOnce()
    {
        var memberId = Guid.NewGuid();
        var pairs    = Enumerable.Range(0, 3).Select(_ => CreateActivePair(memberId)).ToList();
        SetupHappyPath(memberId, pairs.Select(p => p.loan), pairs.Select(p => p.book));

        await Sut().Handle(new GetActiveLoansByMemberQuery(), default);

        _loanRepo.Verify(r => r.GetActiveLoansByMemberAsync(memberId,
                                                             It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private void SetupHappyPath(Guid memberId, IEnumerable<Loan> loans, IEnumerable<Book> books)
    {
        _currentUser.Setup(u => u.UserId).Returns(memberId);
        _loanRepo.Setup(r => r.GetActiveLoansByMemberAsync(memberId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(loans.ToList());
        _bookRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(books.ToList());
    }
}
