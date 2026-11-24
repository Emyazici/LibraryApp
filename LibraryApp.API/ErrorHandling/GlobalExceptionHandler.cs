using FluentValidation;
using LibraryApp.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApp.API.ErrorHandling;

/// <summary>
/// Yakalanmamış exception'ları RFC 7807 (application/problem+json) yanıtlarına çevirir.
/// Beklenmeyen hatalarda exception detayı istemciye sızdırılmaz, sadece loglanır.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException validationException => CreateValidationProblem(validationException),
            BusinessRuleException => CreateProblem(StatusCodes.Status422UnprocessableEntity, "Business rule violation", exception.Message),
            NotFoundException => CreateProblem(StatusCodes.Status404NotFound, "Resource not found", exception.Message),
            UnauthorizedException => CreateProblem(StatusCodes.Status403Forbidden, "Forbidden", exception.Message),
            _ => null
        };

        if (problemDetails is null)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

            problemDetails = CreateProblem(StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null);
        }
        else
        {
            logger.LogInformation("Handled {ExceptionType} as {StatusCode}: {Message}",
                exception.GetType().Name, problemDetails.Status, exception.Message);
        }

        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private static ValidationProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred."
        };
    }

    private static ProblemDetails CreateProblem(int status, string title, string? detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail
    };
}
