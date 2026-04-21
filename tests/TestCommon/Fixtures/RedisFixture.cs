using DotNet.Testcontainers.Builders;
using Testcontainers.Redis;
using Xunit;
using Xunit.Sdk;

namespace Tokenization.Tests.Shared.Fixtures;

public sealed class RedisFixture : IAsyncLifetime
{
    private RedisContainer? _container;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new RedisBuilder()
                .WithImage("redis:7-alpine")
                .WithPortBinding(6379, true)
                .Build();

            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
        }
        catch (DockerUnavailableException)
        {
            throw SkipException.ForSkip("Docker is required for integration tests. Start Docker and rerun the integration suite.");
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    public string ConnectionString { get; private set; } = string.Empty;
}
