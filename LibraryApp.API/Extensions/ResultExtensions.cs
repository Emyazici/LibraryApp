using LibraryApp.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApp.API.Extensions;

/// <summary>
/// Application katmanının <see cref="Result"/> tipini HTTP yanıtına çeviren tek nokta.
/// Başarılı Result&lt;T&gt; → 200 + değer, başarılı Result → 204, başarısız → 400 ProblemDetails.
/// </summary>
public static class ResultExtensions
{
    private const string FailureTitle = "Request failed";
    private const string ProblemJsonContentType = "application/problem+json";

    public static ActionResult ToActionResult(this Result result) =>
        result.IsSuccess ? new NoContentResult() : Failure(result.Error);

    public static ActionResult<T> ToActionResult<T>(this Result<T> result) =>
        result.IsSuccess ? new OkObjectResult(result.Value) : Failure(result.Error);

    private static ObjectResult Failure(string error)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = FailureTitle,
            Detail = error
        };

        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { ProblemJsonContentType }
        };
    }
}
