using Microsoft.Extensions.Caching.Hybrid;
using Xunit;

namespace Tokenization.Tests.Shared.Fixtures;

public sealed class HybridCacheFixture : IAsyncLifetime
{
    private RedisFixture? _redisFixture;

    private ServiceProvider? _provider;

    public HybridCache? Cache { get; private set; }

    public async Task InitializeAsync()
    {
        _redisFixture = new RedisFixture();
        await _redisFixture.InitializeAsync();

        ResetServiceCollection();

        Cache = _provider?.GetRequiredService<HybridCache>();
    }

    public void ResetServiceCollection()
    {
        SetServiceCollection(_redisFixture?.ConnectionString);
    }

    public void SetServiceCollection(string? connectionString)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
        });
        services.AddHybridCache();

        _provider = services.BuildServiceProvider();
        Cache = _provider.GetRequiredService<HybridCache>();
    }

    public async Task DisposeAsync()
    {
        if (_redisFixture is not null)
        {
            await _redisFixture.DisposeAsync();
        }
    }
}
