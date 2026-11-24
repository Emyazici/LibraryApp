using LibraryApp.Application.Common;
using MediatR;

namespace LibraryApp.Application.Commands.Login;

// Uygulama şifre doğrulamasını kendisi yapmaz; kimlik sağlayıcıya (Keycloak) devreder.
public class LoginCommandHandler(IAuthTokenService authTokenService)
    : IRequestHandler<LoginCommand, Result<AuthTokenDto>>
{
    public Task<Result<AuthTokenDto>> Handle(LoginCommand request, CancellationToken ct) =>
        authTokenService.RequestTokenAsync(request.Username, request.Password, ct);
}
