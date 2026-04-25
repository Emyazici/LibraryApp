namespace LibraryApp.Application.Queries.GetBookById;

public record BookDto(
    Guid    Id,
    string  Title,
    string  AuthorId,
    string  Isbn,      // ISBN Value Object → string
    string  Status,    // BookStatus enum → string
    int     Stock,
    decimal Price,     // Money.Amount
    string  Currency   // Money.Currency
);