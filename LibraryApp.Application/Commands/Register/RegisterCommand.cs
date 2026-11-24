using LibraryApp.Application.Common;
using MediatR;

namespace LibraryApp.Application.Commands.Register;

public record RegisterCommand(
    string Name,
    string Surname,
    string Email,
    string Password
) : IRequest<Result<Guid>>;
