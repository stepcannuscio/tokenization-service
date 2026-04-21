using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Tokenization.Tests.Shared.Fixtures;

public sealed class HybridCacheFixtureInMemory : IDisposable
{
    private ServiceProvider _provider;
    public HybridCache Cache { get; private set; }

    public HybridCacheFixtureInMemory()
    {
        var services = GetServices();
        _provider = services.BuildServiceProvider();
        Cache = _provider.GetRequiredService<HybridCache>();
    }
    
    public void SetValidCache()
    {
        var services = GetServices();
        _provider = services.BuildServiceProvider();
        Cache = _provider.GetRequiredService<HybridCache>();
    }
    
    public void SetInvalidCache()
    {
        var services = GetServices();
        services.RemoveAll<IMemoryCache>();
        var mock = new Mock<IMemoryCache>();
        object? result;
        mock.Setup(c => c.TryGetValue(It.IsAny<object>(), out result))
            .Throws<InvalidOperationException>();
        services.AddSingleton(mock.Object);
        
        _provider = services.BuildServiceProvider();
        Cache = _provider.GetRequiredService<HybridCache>();
    }
    
    private static ServiceCollection GetServices()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddMemoryCache();
        services.AddHybridCache();
        return services;
    }

    public void Dispose() => _provider?.Dispose();
}