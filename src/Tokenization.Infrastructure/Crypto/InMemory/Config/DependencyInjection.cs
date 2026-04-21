using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Crypto.Caching;
using Tokenization.Infrastructure.Crypto.Enums;

namespace Tokenization.Infrastructure.Crypto.InMemory.Config;

/// <summary>
/// DI extensions to register the in-memory key-management stack (provider + cache).
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Registers InMemory infrastructure for the <see cref="IKeyProvider"/>.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> to enable fluent chaining.</returns>
    public static IServiceCollection AddInMemoryInfra(this IServiceCollection services)
    {
        services.AddSingleton<KeyClientCache<InMemoryKeyClient, byte[]>>();
        services.AddKeyedSingleton<IKeyProvider>(KeyProviderType.InMemory, (sp, _) =>
        {
            var cache = sp.GetRequiredService<KeyClientCache<InMemoryKeyClient, byte[]>>();
            return new InMemoryKeyProvider(cache);
        });

        return services;
    }
        
    /// <summary>
    /// Adds InMemory health checks to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddInMemoryHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<InMemoryKeyProviderHealthCheck>(
                "inmemory-keyprovider",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["inmemory", "crypto", "infrastructure"]);
        
        return services;
    }
}