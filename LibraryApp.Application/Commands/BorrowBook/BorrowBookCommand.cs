using MediatR;
using LibraryApp.Application.Common;

namespace LibraryApp.Application.Commands.BorrowBook;

public record class BorrowBookCommand(
    Guid BookId,
    Guid MemberId,
    DateTime Start,
	DateTime Due
) : IRequest<Result<Guid>>;