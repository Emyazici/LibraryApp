using LibraryApp.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApp.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult FromResult(Result result)
    {
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    protected ActionResult<T> FromResult<T>(Result<T> result)
    {
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
