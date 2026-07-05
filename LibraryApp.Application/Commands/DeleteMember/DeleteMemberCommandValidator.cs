using FluentValidation;

namespace LibraryApp.Application.Commands.DeleteMember;

public class DeleteMemberCommandValidator : AbstractValidator<DeleteMemberCommand>
{
    public DeleteMemberCommandValidator()
    {
        RuleFor(x => x.MemberId)
            .NotEmpty()
            .WithMessage("MemberId boş olamaz.");
    }
}
