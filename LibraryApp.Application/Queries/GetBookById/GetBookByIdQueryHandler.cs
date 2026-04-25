using LibraryApp.Application.Common;
using LibraryApp.Domain.Repositories;
using MediatR;

namespace LibraryApp.Application.Queries.GetBookById;

public class GetBookByIdQueryHandler
    : IRequestHandler<GetBookByIdQuery, Result<BookDto>>
{
    private readonly IBookRepository _bookRepository;

    public GetBookByIdQueryHandler(IBookRepository bookRepository)
        => _bookRepository = bookRepository;

    public async Task<Result<BookDto>> Handle(
        GetBookByIdQuery  query,
        CancellationToken ct)
    {
        // 1. Book'u çek
        var book = await _bookRepository
            .GetByIdAsync(query.BookId, ct);

        if (book is null)
            return Result.Failure<BookDto>("Kitap bulunamadı.");

        // 2. Aggregate → DTO — aggregate metodu çağırmıyoruz!
        var dto = new BookDto(
            book.Id,
            book.Title,
            book.AuthorId.ToString(),
            book.Isbn.Value,          // ISBN VO → string
            book.Status.ToString(),    // Enum → string
            book.TotalStock,
            book.Money.Amount,         // Money VO → decimal
            book.Money.Currency        // Money VO → string
        );

        // 3. Dön
        return Result<BookDto>.Success(dto);
    }
}