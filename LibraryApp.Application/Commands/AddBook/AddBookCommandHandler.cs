using LibraryApp.Application.Common;
using LibraryApp.Domain.Repositories;
using LibraryApp.Domain.Entities;
using MediatR;

namespace LibraryApp.Application.Commands.AddBook;

public class AddBookCommandHandler : IRequestHandler<AddBookCommand, Result<Guid>>
{
	private readonly IBookRepository _bookRepository;
	private readonly IUnitOfWork _unitOfWork;

	public AddBookCommandHandler(IBookRepository bookRepository, IUnitOfWork unitOfWork)
	{
		_bookRepository = bookRepository;
		_unitOfWork = unitOfWork;
	}

	public async Task<Result<Guid>> Handle(AddBookCommand request, CancellationToken ct)
	{

		var exists = await _bookRepository.ExistsByIsbnAsync(request.ISBN, ct);
		if(exists)
			return Result.Failure<Guid>("Bu ISBN numarasıyla zaten bir kitap mevcut.");
			
		var book = Book.Create(
			request.AuthorId, 
			request.Title,
			request.ISBN,
			request.Price,
			request.Currency,
			request.TotalStock
			);
		
		await _bookRepository.AddAsync(book,ct);
		await _unitOfWork.SaveChangesAsync(ct);

		return Result<Guid>.Success(book.Id);
	}
}