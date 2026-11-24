using LibraryApp.Application.Common;
using MediatR;

namespace LibraryApp.Application.Commands.Login;

public record LoginCommand(string Username, string Password) : IRequest<Result<AuthTokenDto>>;
