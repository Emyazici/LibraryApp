using LibraryApp.Application.Common;
using LibraryApp.Application.Queries.GetActiveLoansByMember;
using LibraryApp.Domain.Repositories;
using MediatR;

namespace LibraryApp.Application.Queries.GetLoanHistoryByMember;

public class GetLoanHistoryByMemberQueryHandler
    : IRequestHandler<GetLoanHistoryByMemberQuery, Result<List<LoanDto>>>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IBookRepository _bookRepository;
    private readonly ICurrentUserService _currentUser;

    public GetLoanHistoryByMemberQueryHandler(
        ILoanRepository loanRepository,
        IBookRepository bookRepository,
        ICurrentUserService currentUser)
    {
        _loanRepository = loanRepository;
        _bookRepository = bookRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<List<LoanDto>>> Handle(
        GetLoanHistoryByMemberQuery query,
        CancellationToken ct)
    {
        // Üyenin tüm loan geçmişi — status filtresi yok (Active/Returned/Overdue hepsi)
        var memberId = _currentUser.UserId;

        var loans = await _loanRepository.GetByMemberIdAsync(memberId, ct);

        var bookIds = loans.Select(l => l.BookId).ToList();
        var books = await _bookRepository.GetByIdsAsync(bookIds, ct);
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
