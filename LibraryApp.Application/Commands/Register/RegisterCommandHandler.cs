using LibraryApp.Application.Common;
using LibraryApp.Domain.Entities;
using LibraryApp.Domain.Repositories;
using MediatR;

namespace LibraryApp.Application.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<Guid>>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKeycloakAdminClient _keycloakAdminClient;

    public RegisterCommandHandler(
        IMemberRepository memberRepository,
        IUnitOfWork unitOfWork,
        IKeycloakAdminClient keycloakAdminClient)
    {
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
        _keycloakAdminClient = keycloakAdminClient;
    }

    public async Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var emailExists = await _memberRepository.ExistsByEmailAsync(request.Email, ct);
        if (emailExists)
            return Result.Failure<Guid>("Bu e-posta adresiyle zaten bir üyelik mevcut.");

        var keycloakResult = await _keycloakAdminClient.CreateUserAsync(
            username: request.Email,
            email: request.Email,
            firstName: request.Name,
            lastName: request.Surname,
            password: request.Password,
            realmRole: "Customer",
            ct: ct);

        if (keycloakResult.IsFailure)
            return Result.Failure<Guid>(keycloakResult.Error);

        var member = Member.Create(request.Name, request.Surname, request.Email, keycloakResult.Value);

        await _memberRepository.AddAsync(member, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(member.Id);
    }
}
