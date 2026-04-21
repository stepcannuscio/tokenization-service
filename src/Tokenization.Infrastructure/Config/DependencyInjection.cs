using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Caching.Config;
using Tokenization.Infrastructure.Caching.Config.Options;
using Tokenization.Infrastructure.Caching.Health.Config;
using Tokenization.Infrastructure.Crypto.Config;
using Tokenization.Infrastructure.Crypto.Health.Config;
using Tokenization.Infrastructure.Db.Config;
using Tokenization.Infrastructure.Db.Health.Config;
using Tokenization.Infrastructure.Config.Options;
using Tokenization.Infrastructure.Crypto.Services;
using Tokenization.Infrastructure.Db.Config.Options;
using Tokenization.Infrastructure.Services;

namespace Tokenization.Infrastructure.Config;

/// <summary>
/// Dependency injection extensions for wiring Tokenization infrastructure into an application.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Registers Tokenization infrastructure services with the provided service collection.
    /// </summary>
    /// <param name="services">The application service collection to add services to.</param>
    /// <param name="config">The configuration root used to bind settings required by the infrastructure (e.g., crypto/key management).</param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so calls can be chained.
    /// </returns>
    /// <remarks>
    /// Typical usage (e.g., in <c>Program.cs</c>):
    /// <code>
    /// builder.Services.AddTokenizationInfra(builder.Configuration);
    /// </code>
    /// This method is intended to remain a stable entry point as additional infrastructure components are added.
    /// </remarks>
    public static IServiceCollection AddTokenizationInfra(this IServiceCollection services,
        IConfiguration config)
    {
        services.AddOptions<KeyStorageOptions>()
            .Bind(config.GetSection(KeyStorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        services.AddCryptoInfra(config);
        services.AddDbInfra(config);
        services.AddCachingInfra(config);
        services.AddScoped<ITenantContextService, TenantContextService>();
        
        services.AddScoped<IEncryptionService>(sp =>
        {
            var keyStorageOptions = config.GetSection(KeyStorageOptions.SectionName).Get<KeyStorageOptions>();
            var keyProvider = sp.GetRequiredService<IKeyProvider>();
            return new EncryptionService(keyProvider, keyStorageOptions?.KekKeyName ?? string.Empty);
        });
        
        // Register health checks
        services.AddHealthChecks();
        
        // Add database health check
        var dbOptions = config.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>();
        if (dbOptions != null)
        {
            services.AddDbHealthCheck(dbOptions);
        }
        
        // Add cache health check
        var cacheOptions = config.GetSection(CacheOptions.SectionName).Get<CacheOptions>();
        if (cacheOptions != null)
        {
            services.AddCacheHealthCheck(cacheOptions);
        }
        
        // Add crypto health checks
        var keyStorageOptions = config.GetSection(KeyStorageOptions.SectionName).Get<KeyStorageOptions>();
        if (keyStorageOptions != null)
        {
            services.AddCryptoHealthChecks(keyStorageOptions);
        }
        
        return services;
    }
}
