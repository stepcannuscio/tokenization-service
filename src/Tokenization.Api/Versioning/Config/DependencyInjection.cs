namespace Tokenization.Api.Versioning.Config;

/// <summary>
/// DI registration for API versioning.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Configures API versioning.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void AddTokenizationVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(_ => ApiVersioningPolicies.GetVersioningOptions());
    }
}