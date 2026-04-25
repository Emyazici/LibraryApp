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

        // 2. Aktif loan'ları çek
        var loans = await _loanRepository
            .GetByMemberIdAsync(memberId, ct);

        // 3. Her loan için BookTitle'ı da çek
        var dtos = new List<LoanDto>();

        foreach (var loan in loans)
        {
            var book = await _bookRepository
                .GetByIdAsync(loan.BookId, ct);

            dtos.Add(new LoanDto(
                loan.Id,
                loan.BookId,
                book?.Title ?? "Bilinmiyor",
                loan.Period.BorrowedAt,
                loan.Period.ExpectedReturnDate,
                loan.Status.ToString(),
                loan.Period.IsOverdue(),
                loan.Fee.Amount,
                loan.Fee.Currency
            ));
        }

        return Result<List<LoanDto>>.Success(dtos);
    }
}