using LibraryApp.Application.Common;
using LibraryApp.Application.Queries.GetActiveLoansByMember;
using MediatR;

namespace LibraryApp.Application.Queries.GetLoanHistoryByMember;

public record GetLoanHistoryByMemberQuery() : IRequest<Result<List<LoanDto>>>;
