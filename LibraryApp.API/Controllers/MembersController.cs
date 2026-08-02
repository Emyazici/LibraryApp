using LibraryApp.Application.Commands.DeleteMember;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApp.API.Controllers;

[Route("api/[controller]")]
[Authorize]
public class MembersController : ApiControllerBase
{
    private readonly ISender _sender;
    public MembersController(ISender sender) => _sender = sender;

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteMemberCommand(id), ct);
        return FromResult(result);
    }
}
