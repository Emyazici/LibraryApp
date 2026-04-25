using FluentValidation;

namespace LibraryApp.Application.Commands.ReturnBook;

public class ReturnBookCommandValidator
    : AbstractValidator<ReturnBookCommand>
{
    public ReturnBookCommandValidator()
    {
        RuleFor(x => x.LoanId)
            .NotEmpty()
            .WithMessage("LoanId boş olamaz.");
    }
}