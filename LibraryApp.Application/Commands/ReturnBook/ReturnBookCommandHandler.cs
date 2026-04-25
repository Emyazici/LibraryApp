using LibraryApp.Application.Common;
using LibraryApp.Domain.Repositories;
using MediatR;

namespace LibraryApp.Application.Commands.ReturnBook;

public class ReturnBookCommandHandler
    : IRequestHandler<ReturnBookCommand, Result>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IBookRepository _bookRepository;
	private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork     _unitOfWork;

    public ReturnBookCommandHandler(
        ILoanRepository loanRepository,
        IBookRepository bookRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork     unitOfWork)
    {
        _loanRepository = loanRepository;
        _bookRepository = bookRepository;
        _currentUserService = currentUserService;
        _unitOfWork     = unitOfWork;
    }

    public async Task<Result> Handle(
        ReturnBookCommand command,
        CancellationToken ct)
    {
        // 1. Loan'ı çek
        var loan = await _loanRepository
            .GetByIdAsync(command.LoanId, ct);

        if (loan is null)
            return Result.Failure("Ödünç kaydı bulunamadı.");

        if (loan.MemberId != _currentUserService.UserId)
            return Result.Failure("Bu ödünç size ait değil.");

        // 2. Book'u çek — stok ve status güncellenmeli
        var book = await _bookRepository
            .GetByIdAsync(loan.BookId, ct);

        if (book is null)
            return Result.Failure("Kitap bulunamadı.");

        // 3. Loan aggregate — iş kuralı burada
        // IsOverdue kontrolü, LateFee hesaplama, Status güncelleme
        loan.Return();
		await _loanRepository.UpdateAsync(loan, ct);

        // 4. Book aggregate — stok ve status güncelle
        book.Return();
		await _bookRepository.UpdateAsync(book, ct);

        // 5. Kaydet — BookReturnedEvent burada yayınlanır
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}