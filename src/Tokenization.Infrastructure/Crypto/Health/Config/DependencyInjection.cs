using Microsoft.Extensions.DependencyInjection;
using Tokenization.Infrastructure.Config.Options;
using Tokenization.Infrastructure.Crypto.Enums;
using Tokenization.Infrastructure.Crypto.InMemory.Config;
using Tokenization.Infrastructure.Crypto.KeyVault.Config;

namespace Tokenization.Infrastructure.Crypto.Health.Config;

/// <summary>
/// DI registration for the crypto health checks.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Adds crypto health checks to the service collection based on the configured key provider type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Key storage options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCryptoHealthChecks(
        this IServiceCollection services,
        KeyStorageOptions options)
    {
        if (!options.EnableHealthChecks)
            return services;

        services.AddHealthChecks();

        // Add health check based on the configured key provider type
        switch (options.KeyProvider)
        {
            case KeyProviderType.AzureKeyVault:
                services.AddKeyVaultHealthChecks();
                break;

            case KeyProviderType.InMemory:
                services.AddInMemoryHealthChecks();
                break;

            default:
                throw new InvalidOperationException($"Unsupported key provider type: {options.KeyProvider}");
        }

        return services;
    }
}