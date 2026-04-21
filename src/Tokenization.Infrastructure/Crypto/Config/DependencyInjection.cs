using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Config.Options;
using Tokenization.Infrastructure.Crypto.Background;
using Tokenization.Infrastructure.Crypto.Enums;
using Tokenization.Infrastructure.Crypto.InMemory.Config;
using Tokenization.Infrastructure.Crypto.KeyVault.Config;

namespace Tokenization.Infrastructure.Crypto.Config;

/// <summary>
/// DI registration entry points for the crypto infrastructure.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Registers the crypto infrastructure: key provider (in-memory or Key Vault), background preloader, and encryption service.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="config">Configuration root>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddCryptoInfra(this IServiceCollection services, IConfiguration config)
    {
        services.AddKeyProvider(config);
        services.AddKeyPreloader(config);

        return services;
    }

    /// <summary>
    /// Registers the <see cref="IKeyProvider"/> according to configured <see cref="KeyProviderType"/>.
    /// </summary>
    /// <remarks>
    /// The provider is resolved via keyed services to ensure only the selected implementation is constructed.
    /// </remarks>
    private static void AddKeyProvider(this IServiceCollection services, IConfiguration config)
    {
        var keyStorageOptions = config.GetSection(KeyStorageOptions.SectionName).Get<KeyStorageOptions>();
        switch (keyStorageOptions?.KeyProvider)
        {
            case KeyProviderType.InMemory:
            {
                services.AddInMemoryInfra();
                break;
            }
            case KeyProviderType.AzureKeyVault:
            {
                services.AddKeyVaultInfra();
                break;
            }
            default:
                throw new InvalidOperationException("Key provider type is invalid");
        }

        services.AddSingleton<IKeyProvider>(sp =>
            sp.GetRequiredKeyedService<IKeyProvider>(keyStorageOptions.KeyProvider));
    }

    /// <summary>
    /// Adds key preloader hosted service that runs at application startup.
    /// </summary>
    private static void AddKeyPreloader(this IServiceCollection services, IConfiguration config)
    {
        services.AddHostedService<KeyPreloaderHostedService>(sp =>
        {
            var keyStorageOptions = config.GetSection(KeyStorageOptions.SectionName).Get<KeyStorageOptions>();
            var keyNames = new List<string>
                {
                    keyStorageOptions?.KekKeyName ?? string.Empty,
                    keyStorageOptions?.BlindIndexKeyName ?? string.Empty
                }
                .Where(key => !string.IsNullOrEmpty(key))
                .ToList();

            var keyProvider = sp.GetRequiredService<IKeyProvider>();
            var logger = sp.GetRequiredService<ILogger<KeyPreloaderHostedService>>();
            return new KeyPreloaderHostedService(logger, keyNames, keyProvider);
        });
    }
}