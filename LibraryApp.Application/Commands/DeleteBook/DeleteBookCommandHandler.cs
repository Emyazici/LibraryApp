using LibraryApp.Application.Common;
using LibraryApp.Domain.Repositories;
using MediatR;

namespace LibraryApp.Application.Commands.DeleteBook;

public class DeleteBookCommandHandler : IRequestHandler<DeleteBookCommand, Result>
{
    private readonly IBookRepository _bookRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBookCommandHandler(IBookRepository bookRepository, IUnitOfWork unitOfWork)
    {
        _bookRepository = bookRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteBookCommand request, CancellationToken ct)
    {
        var book = await _bookRepository.GetByIdAsync(request.BookId, ct);
        if (book is null)
            return Result.Failure("Kitap bulunamadı.");

        await _bookRepository.DeleteAsync(request.BookId, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
