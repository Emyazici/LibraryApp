using LibraryApp.Application.Common;
using MediatR;

namespace LibraryApp.Application.Queries.GetBookById;

public record GetBookByIdQuery(
    Guid BookId
) : IRequest<Result<BookDto>>;