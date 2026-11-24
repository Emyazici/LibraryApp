using LibraryApp.Application.Common;
using LibraryApp.Application.Queries.GetBookById;
using LibraryApp.Domain.Repositories;
using MediatR;

namespace LibraryApp.Application.Queries.GetBooks;

public class GetBooksQueryHandler(IBookRepository bookRepository)
    : IRequestHandler<GetBooksQuery, Result<List<BookDto>>>
{
    public async Task<Result<List<BookDto>>> Handle(GetBooksQuery query, CancellationToken ct)
    {
        var books = await bookRepository.GetAllAsync(ct);

        var dtos = books.Select(book => new BookDto(
            book.Id,
            book.Title,
            book.AuthorId.ToString(),
            book.Isbn.Value,
            book.Status.ToString(),
            book.TotalStock,
            book.Money.Amount,
            book.Money.Currency
        )).ToList();

        return Result<List<BookDto>>.Success(dtos);
    }
}
