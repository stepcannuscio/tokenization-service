using Testcontainers.MsSql;
using Testcontainers.Redis;
using Xunit;

namespace Tokenization.Tests.Shared.Fixtures;

/// <summary>
/// Collection fixture that manages shared Docker containers for all integration tests.
/// This ensures that all integration tests share the same SQL Server and Redis containers,
/// preventing conflicts when tests run in parallel.
/// </summary>
[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestCollectionFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public sealed class IntegrationTestCollectionFixture : IAsyncLifetime
{
    private static readonly SemaphoreSlim InitializationSemaphore = new(1, 1);
    private static IntegrationTestCollectionFixture? _instance;
    private static bool _isInitialized;

    private MsSqlContainer? _sqlServerContainer;
    private RedisContainer? _redisContainer;
    private string _sqlServerConnectionString = string.Empty;
    private string _redisConnectionString = string.Empty;

    public string SqlServerConnectionString => _sqlServerConnectionString;
    public string RedisConnectionString => _redisConnectionString;

    public static IntegrationTestCollectionFixture Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new IntegrationTestCollectionFixture();
            }
            return _instance;
        }
    }

    public async Task InitializeAsync()
    {
        await InitializationSemaphore.WaitAsync();
        try
        {
            if (_isInitialized)
            {
                return;
            }

            // Create and start containers
            _sqlServerContainer = new MsSqlBuilder()
                .WithName($"tokenization-sqlserver-{Guid.NewGuid():N}")
                .Build();

            _redisContainer = new RedisBuilder()
                .WithImage("redis:7-alpine")
                .WithName($"tokenization-redis-{Guid.NewGuid():N}")
                .WithPortBinding(6379, true)
                .Build();

            // Start containers in parallel
            var startTasks = new List<Task>
            {
                _sqlServerContainer.StartAsync(),
                _redisContainer.StartAsync()
            };

            await Task.WhenAll(startTasks);

            _sqlServerConnectionString = _sqlServerContainer.GetConnectionString();
            _redisConnectionString = _redisContainer.GetConnectionString();

            _isInitialized = true;
        }
        finally
        {
            InitializationSemaphore.Release();
        }
    }

    public async Task DisposeAsync()
    {
        await InitializationSemaphore.WaitAsync();
        try
        {
            if (!_isInitialized)
            {
                return;
            }

            var disposeTasks = new List<Task>();

            if (_sqlServerContainer != null)
            {
                disposeTasks.Add(_sqlServerContainer.StopAsync());
                disposeTasks.Add(_sqlServerContainer.DisposeAsync().AsTask());
            }

            if (_redisContainer != null)
            {
                disposeTasks.Add(_redisContainer.StopAsync());
                disposeTasks.Add(_redisContainer.DisposeAsync().AsTask());
            }

            if (disposeTasks.Count > 0)
            {
                await Task.WhenAll(disposeTasks);
            }

            _isInitialized = false;
            _instance = null;
        }
        finally
        {
            InitializationSemaphore.Release();
        }
    }

    /// <summary>
    /// Creates a unique database name for test isolation.
    /// Each test should use its own database to avoid conflicts.
    /// </summary>
    public string CreateUniqueDatabaseName() => $"TestDb_{Guid.NewGuid():N}";
}
