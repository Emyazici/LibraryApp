using LibraryApp.Application.Common;
using MediatR;

namespace LibraryApp.Application.Queries.GetActiveLoansByMember;

public record GetActiveLoansByMemberQuery()
    : IRequest<Result<List<LoanDto>>>;