using Tokenization.Api.Idempotency.Config.Options;

namespace Tokenization.Api.Idempotency.Config;

/// <summary>
/// DI registration for idempotency.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Configures idempotency services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    public static void AddTokenizationIdempotency(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IdempotencyOptions>(configuration.GetSection(IdempotencyOptions.SectionName));
        services.AddSingleton<IIdempotencyKeyHasher, DefaultIdempotencyKeyHasher>();
    }

    /// <summary>
    /// Configures idempotency middleware.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static void UseTokenizationIdempotency(this WebApplication app)
    {
        app.UseMiddleware<IdempotencyMiddleware>();
    }
}
