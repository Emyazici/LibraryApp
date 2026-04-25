using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LibraryApp.Application.Common.Behaviors;

public class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        var response = await next();

        sw.Stop();

        if (sw.ElapsedMilliseconds > 500)
            logger.LogWarning(
                "Slow request detected: {RequestName} took {ElapsedMs}ms",
                typeof(TRequest).Name,
                sw.ElapsedMilliseconds);

        return response;
    }
}
