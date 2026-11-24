using FluentValidation;

namespace LibraryApp.Application.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Kullanıcı adı boş olamaz.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Şifre boş olamaz.");
    }
}
