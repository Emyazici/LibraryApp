using LibraryApp.Application.Common;
using LibraryApp.Domain.Repositories;
using MediatR;

namespace LibraryApp.Application.Queries.GetActiveLoansByMember;

public class GetActiveLoansByMemberQueryHandler
    : IRequestHandler<GetActiveLoansByMemberQuery, Result<List<LoanDto>>>
{
    private readonly ILoanRepository     _loanRepository;
    private readonly IBookRepository     _bookRepository;
    private readonly ICurrentUserService  _currentUser;

    public GetActiveLoansByMemberQueryHandler(
        ILoanRepository    loanRepository,
        IBookRepository    bookRepository,
        ICurrentUserService currentUser)
    {
        _loanRepository = loanRepository;
        _bookRepository = bookRepository;
        _currentUser    = currentUser;
    }

    public async Task<Result<List<LoanDto>>> Handle(
        GetActiveLoansByMemberQuery query,
        CancellationToken          ct)
    {
        // 1. MemberId token'dan geliyor
        var memberId = _currentUser.UserId;

        // 2. Sadece aktif loan'ları çek (WHERE Status = Active)
        var loans = await _loanRepository
            .GetActiveLoansByMemberAsync(memberId, ct);

        // 3. Tüm kitapları tek sorguda çek — N+1 yok
        var bookIds = loans.Select(l => l.BookId).ToList();
        var books   = await _bookRepository.GetByIdsAsync(bookIds, ct);
        var bookMap = books.ToDictionary(b => b.Id);

        var dtos = loans.Select(loan =>
        {
            bookMap.TryGetValue(loan.BookId, out var book);
            return new LoanDto(
                loan.Id,
                loan.BookId,
                book?.Title ?? "Bilinmiyor",
                loan.Period.BorrowedAt,
                loan.Period.ExpectedReturnDate,
                loan.Status.ToString(),
                loan.Period.IsOverdue(),
                loan.Fee.Amount,
                loan.Fee.Currency
            );
        }).ToList();

        return Result<List<LoanDto>>.Success(dtos);
    }
}