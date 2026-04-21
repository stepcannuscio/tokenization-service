using Azure.Security.KeyVault.Keys;
using Microsoft.Extensions.Caching.Hybrid;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Caching;
using Tokenization.Infrastructure.Config.Options;
using Tokenization.Infrastructure.Crypto.Enums;
using Tokenization.Infrastructure.Crypto.KeyVault.Config;
using Xunit;

namespace Tokenization.Tests.Shared.Fixtures;

/// <summary>
/// Fixture for Azure Key Vault integration tests.
/// These tests are opt-in and expect local configuration or environment variables to be present.
/// </summary>
public sealed class KeyVaultFixture : IAsyncLifetime
{
    private ServiceProvider? ServiceProvider { get; set; }
    private HybridCacheFixture? HybridCacheFixture { get; set; }
    private IKeyProvider? KeyVaultProvider { get; set; }
    
    public KeyClient? KeyClient { get; private set; }
    public string? VaultUrl { get; private set; }
    public string? KekKeyName { get; private set; }
    public string? BlindIndexKeyName { get; private set; }
    public HybridCache? Cache { get; set; }
    
    public async Task InitializeAsync()
    {
        HybridCacheFixture = new HybridCacheFixture();
        await HybridCacheFixture.InitializeAsync();

        IConfiguration configuration =
            new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var keyStorage = configuration.GetSection(KeyStorageOptions.SectionName).Get<KeyStorageOptions>() ??
                         throw new InvalidOperationException("Key storage configuration not found");
        
        VaultUrl = keyStorage.VaultUrl;
        KekKeyName = keyStorage.KekKeyName;
        BlindIndexKeyName = keyStorage.BlindIndexKeyName;

        // Set up services
        var services = new ServiceCollection();
        
        // Add configuration
        services.AddSingleton(configuration);
        services.AddOptions<KeyStorageOptions>()
            .Bind(configuration.GetSection(KeyStorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        services.AddSingleton<ICacheKeyGenerator, CacheKeyGenerator>();
        
        // Add cache infrastructure using HybridCacheFixture
        services.AddSingleton(HybridCacheFixture.Cache ??
                              throw new InvalidOperationException("HybridCache is not available from fixture"));
        
        // Add KeyVault infrastructure
        services.AddKeyVaultInfra();
        
        // Build service provider
        ServiceProvider = services.BuildServiceProvider();
        
        // Get required services
        KeyClient = ServiceProvider.GetRequiredService<KeyClient>();
        KeyVaultProvider = ServiceProvider.GetRequiredKeyedService<IKeyProvider>(KeyProviderType.AzureKeyVault);
    }

    /// <summary>
    /// Creates a scope for the service provider to get scoped services.
    /// </summary>
    public IServiceScope CreateScope()
    {
        return ServiceProvider?.CreateScope() ?? throw new InvalidOperationException("ServiceProvider does not exist.");
    }

    /// <summary>
    /// Gets the KeyVault provider as a specific type for testing.
    /// </summary>
    public T GetKeyVaultProvider<T>() where T : class => (T?)KeyVaultProvider ??
                                                         throw new InvalidOperationException(
                                                             "KeyVaultProvider does not exist.");
    
    public async Task DisposeAsync()
    {
        if (HybridCacheFixture is not null)
        {
            await HybridCacheFixture.DisposeAsync();
        }

        if (ServiceProvider is not null)
        {
            await ServiceProvider.DisposeAsync();
        }
    }
}
