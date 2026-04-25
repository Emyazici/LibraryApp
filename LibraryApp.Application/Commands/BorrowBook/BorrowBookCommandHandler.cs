using LibraryApp.Application.Commands.BorrowBook;
using LibraryApp.Application.Common;
using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Repositories;
using LibraryApp.Domain.ValueObjects;
using MediatR;

namespace LibraryApp.Application.Commands.BorrowBook;

public class BorrowBookCommandHandler
	: IRequestHandler<BorrowBookCommand, Result<Guid>>
{
	private readonly IBookRepository _bookRepository;
	private readonly IMemberRepository _memberRepository;
	private readonly ILoanRepository _loanRepository;
	private readonly IUnitOfWork _unitOfWork;

	public BorrowBookCommandHandler(IBookRepository bookRepository, IMemberRepository memberRepository, ILoanRepository loanRepository, IUnitOfWork unitOfWork)
	{
		_bookRepository = bookRepository;
		_memberRepository = memberRepository;
		_loanRepository = loanRepository;
		_unitOfWork = unitOfWork;
	}

	public async Task<Result<Guid>> Handle(BorrowBookCommand request, CancellationToken cancellationToken)
	{
		var book = await _bookRepository.GetByIdAsync(request.BookId, cancellationToken);

		if (book == null)
			return Result.Failure<Guid>("Kitap bulunamadı.");

		var member = await _memberRepository.ExistsByIdAsync(request.MemberId, cancellationToken);
		if (!member)
			return Result.Failure<Guid>("Üye bulunamadı.");

		var hasActiveLoan = await _loanRepository.HasActiveLoanAsync(request.MemberId, request.BookId, cancellationToken);
		if (hasActiveLoan)
			return Result.Failure<Guid>("Bu üye zaten bu kitabı ödünç almış.");

		book.Borrow();
		await _bookRepository.UpdateAsync(book,cancellationToken);
		var period = LoanPeriod.Create(request.Start, request.Due);
		var loan = Loan.Create(book.Id, request.MemberId, period);

		await _loanRepository.AddAsync(loan, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
		
		return Result.Success(loan.Id);
	}
}