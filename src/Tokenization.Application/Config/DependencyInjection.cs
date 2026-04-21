using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tokenization.Application.Handlers;
using Tokenization.Application.Services;
using Tokenization.Domain.Abstractions;

namespace Tokenization.Application.Config;

/// <summary>
/// DI registration for the application layer (MediatR, validators, mappers, pipeline behaviors).
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Adds the application services (command handlers, validators, behaviors, mappers) to the container.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddTokenizationApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Fluent Validation pipeline behavior
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Services
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
