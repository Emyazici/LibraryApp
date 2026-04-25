using System.Reflection;
using FluentValidation;
using LibraryApp.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryApp.Application;
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                Assembly.GetExecutingAssembly());

            // Sıra önemli! Önce Logging, sonra Validation, sonra Performance
            cfg.AddBehavior(typeof(IPipelineBehavior<,>),
                             typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>),
                             typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>),
                             typeof(PerformanceBehavior<,>));
        });

        var assembly = Assembly.GetExecutingAssembly();
        var validatorTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces(), (t, i) => new { Type = t, Interface = i })
            .Where(x => x.Interface.IsGenericType &&
                        x.Interface.GetGenericTypeDefinition() == typeof(IValidator<>));

        foreach (var item in validatorTypes)
            services.AddTransient(item.Interface, item.Type);

        return services;
    }
}