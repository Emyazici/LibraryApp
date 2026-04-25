using MediatR;
using LibraryApp.Application.Common;

namespace LibraryApp.Application.Commands.AddBook;

public record class AddBookCommand(
	Guid AuthorId,
	string Title,
	string ISBN,
	decimal Price,
	string Currency,
	int TotalStock
) : IRequest<Result<Guid>>;