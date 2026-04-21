using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tokenization.Infrastructure.Caching.Config.Options;

namespace Tokenization.Infrastructure.Caching.Config;

/// <summary>
/// DI registration for the caching infrastructure.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Configures caching services including HybridCache and Redis support.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    public static void AddCachingInfra(this IServiceCollection services, IConfiguration configuration)
    {
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();

        if (!string.IsNullOrEmpty(cacheOptions.RedisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheOptions.RedisConnectionString;
                if (!string.IsNullOrEmpty(cacheOptions.InstanceName))
                {
                    options.InstanceName = cacheOptions.InstanceName;
                }
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddHybridCache();

        services.AddSingleton<ICacheKeyGenerator, CacheKeyGenerator>();
    }
}
