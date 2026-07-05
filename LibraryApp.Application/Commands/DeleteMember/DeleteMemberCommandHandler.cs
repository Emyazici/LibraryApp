using LibraryApp.Application.Common;
using LibraryApp.Domain.Repositories;
using MediatR;

namespace LibraryApp.Application.Commands.DeleteMember;

public class DeleteMemberCommandHandler : IRequestHandler<DeleteMemberCommand, Result>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteMemberCommandHandler(IMemberRepository memberRepository, IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteMemberCommand request, CancellationToken ct)
    {
        var exists = await _memberRepository.ExistsByIdAsync(request.MemberId, ct);
        if (!exists)
            return Result.Failure("Üye bulunamadı.");

        await _memberRepository.DeleteAsync(request.MemberId, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
