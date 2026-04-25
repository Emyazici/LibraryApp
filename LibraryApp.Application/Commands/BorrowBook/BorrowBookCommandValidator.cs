using FluentValidation;

namespace LibraryApp.Application.Commands.BorrowBook;

public class BorrowBookCommandValidator
    : AbstractValidator<BorrowBookCommand>
{
    public BorrowBookCommandValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("BookId boş olamaz.");

        RuleFor(x => x.MemberId)
            .NotEmpty()
            .WithMessage("MemberId boş olamaz.");

        RuleFor(x => x.Due)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Bitiş tarihi bugünden sonra olmalı.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(60))
            .WithMessage("Ödünç süresi 60 günü geçemez.");
    }
}