using FluentValidation;
using MediatR;

namespace LibraryApp.Application.Common.Behaviors;

// Her request MediatR pipeline'ından geçer
// Bu behavior, handler'a ulaşmadan önce validation çalıştırır
// Hata varsa handler hiç çalışmaz
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // DI tüm validator'ları otomatik inject eder
    // CreateOrderCommand gelince → CreateOrderValidator inject edilir
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        // Bu request için validator yoksa geç
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);
        // GlobalExceptionHandler yakalayacak → 400 Bad Request

        return await next();
    }
}
