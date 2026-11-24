using LibraryApp.Application.Common;
using LibraryApp.Application.Queries.GetBookById;
using MediatR;

namespace LibraryApp.Application.Queries.GetBooks;

public record GetBooksQuery : IRequest<Result<List<BookDto>>>;
