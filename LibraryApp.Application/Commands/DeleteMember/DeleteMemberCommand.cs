using LibraryApp.Application.Common;
using MediatR;

namespace LibraryApp.Application.Commands.DeleteMember;

public record DeleteMemberCommand(Guid MemberId) : IRequest<Result>;
