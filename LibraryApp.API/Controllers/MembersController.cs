using LibraryApp.API.Extensions;
using LibraryApp.Application.Commands.DeleteMember;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApp.API.Controllers;

[ApiController]
[Route("api/members")]
[Authorize]
public class MembersController(ISender sender) : ControllerBase
{
    /// <summary>Üyeyi siler (soft delete).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteMemberCommand(id), ct);
        return result.ToActionResult();
    }
}
