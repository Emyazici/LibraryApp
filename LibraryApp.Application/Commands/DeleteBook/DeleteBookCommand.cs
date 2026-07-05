using LibraryApp.Application.Common;
using MediatR;

namespace LibraryApp.Application.Commands.DeleteBook;

public record DeleteBookCommand(Guid BookId) : IRequest<Result>;
