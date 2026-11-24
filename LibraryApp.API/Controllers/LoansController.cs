using LibraryApp.API.Extensions;
using LibraryApp.Application.Commands.BorrowBook;
using LibraryApp.Application.Commands.ReturnBook;
using LibraryApp.Application.Common;
using LibraryApp.Application.Queries.GetActiveLoansByMember;
using LibraryApp.Application.Queries.GetLoanHistoryByMember;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApp.API.Controllers;

public record BorrowBookRequest(Guid BookId, DateTime DueDate);

[ApiController]
[Route("api/loans")]
[Authorize(Roles = "Customer")]
public class LoansController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Giriş yapmış müşteri adına kitap ödünç alır. Oluşan ödünç kaydının id'sini döner.</summary>
    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Borrow(BorrowBookRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new BorrowBookCommand(request.BookId, currentUser.UserId, DateTime.UtcNow, request.DueDate), ct);
        return result.ToActionResult();
    }

    /// <summary>Giriş yapmış müşterinin ödünç aldığı kitabı iade eder.</summary>
    [HttpPost("{id:guid}/return")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Return(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ReturnBookCommand(id), ct);
        return result.ToActionResult();
    }

    /// <summary>Giriş yapmış müşterinin aktif ödünçleri.</summary>
    [HttpGet("me/active")]
    [ProducesResponseType<List<LoanDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LoanDto>>> GetActive(CancellationToken ct)
    {
        var result = await sender.Send(new GetActiveLoansByMemberQuery(), ct);
        return result.ToActionResult();
    }

    /// <summary>Giriş yapmış müşterinin ödünç geçmişi.</summary>
    [HttpGet("me/history")]
    [ProducesResponseType<List<LoanDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LoanDto>>> GetHistory(CancellationToken ct)
    {
        var result = await sender.Send(new GetLoanHistoryByMemberQuery(), ct);
        return result.ToActionResult();
    }
}
