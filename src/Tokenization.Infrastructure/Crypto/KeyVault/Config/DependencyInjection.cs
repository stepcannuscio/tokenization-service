using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Config.Options;
using Tokenization.Infrastructure.Crypto.Caching;
using Tokenization.Infrastructure.Crypto.Enums;

namespace Tokenization.Infrastructure.Crypto.KeyVault.Config;

/// <summary>
/// Dependency injection registration for the Azure Key Vault–backed key provider stack.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Registers KeyVault infrastructure for the <see cref="IKeyProvider"/>.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <remarks>
    /// Reads <see cref="KeyStorageOptions"/> to obtain <c>VaultUrl</c> and <c>KeyName</c>. A
    /// <see cref="DefaultAzureCredential"/> is used by default and registered as a singleton for reuse.
    /// </remarks>
    public static IServiceCollection AddKeyVaultInfra(this IServiceCollection services)
    {
        services.AddSingleton<TokenCredential>(_ => new DefaultAzureCredential());
        services.AddSingleton<KeyClient>(sp =>
        {
            var keyStorageOptions = sp.GetRequiredService<IOptions<KeyStorageOptions>>().Value;
            var credential = sp.GetRequiredService<TokenCredential>();
            return new KeyClient(new Uri(keyStorageOptions.VaultUrl), credential);
        });
        services.AddSingleton<KeyClientCache<KeyVaultKeyMetadata, CryptographyClient>>();
        services.AddSingleton<KeyVaultKeyMetadataFactory>(sp =>
        {
            var credential = sp.GetRequiredService<TokenCredential>();
            return new KeyVaultKeyMetadataFactory(credential);
        });
        
        services.AddKeyedSingleton<IKeyProvider, KeyVaultProvider>(KeyProviderType.AzureKeyVault, (sp, _) =>
        {
            var keyClient = sp.GetRequiredService<KeyClient>();
            var cache = sp.GetRequiredService<KeyClientCache<KeyVaultKeyMetadata, CryptographyClient>>();
            var metadataFactory = sp.GetRequiredService<KeyVaultKeyMetadataFactory>();
            return new KeyVaultProvider(keyClient, cache, metadataFactory);
        });

        return services;
    }
    
    /// <summary>
    /// Adds KeyVault health checks to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddKeyVaultHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<KeyVaultHealthCheck>(
                "keyvault",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["keyvault", "azure", "crypto", "infrastructure"]);
        
        return services;
    }
}