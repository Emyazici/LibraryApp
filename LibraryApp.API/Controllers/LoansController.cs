using LibraryApp.Application.Commands.BorrowBook;
using LibraryApp.Application.Commands.ReturnBook;
using LibraryApp.Application.Queries.GetActiveLoansByMember;
using LibraryApp.Application.Queries.GetLoanHistoryByMember;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApp.API.Controllers;

public record BorrowBookRequest(Guid BookId, Guid MemberId, DateTime Start, DateTime Due);

[Route("api/[controller]")]
[Authorize]
public class LoansController : ApiControllerBase
{
    private readonly ISender _sender;
    public LoansController(ISender sender) => _sender = sender;

    [HttpGet("active")]
    public async Task<ActionResult<List<LibraryApp.Application.Queries.GetActiveLoansByMember.LoanDto>>> GetActive(CancellationToken ct)
    {
        var result = await _sender.Send(new GetActiveLoansByMemberQuery(), ct);
        return FromResult(result);
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<LibraryApp.Application.Queries.GetActiveLoansByMember.LoanDto>>> GetHistory(CancellationToken ct)
    {
        var result = await _sender.Send(new GetLoanHistoryByMemberQuery(), ct);
        return FromResult(result);
    }

    [HttpPost("borrow")]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<ActionResult<Guid>> Borrow(BorrowBookRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(
            new BorrowBookCommand(request.BookId, request.MemberId, request.Start, request.Due), ct);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/return")]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Return(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new ReturnBookCommand(id), ct);
        return FromResult(result);
    }
}
