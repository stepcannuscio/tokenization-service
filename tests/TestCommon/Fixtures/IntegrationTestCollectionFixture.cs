using DotNet.Testcontainers.Builders;
using Testcontainers.MsSql;
using Xunit;
using Xunit.Sdk;

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
    private bool _reuseSqlContainer;
    private static string _sqlServerConnectionString = string.Empty;

    public string SqlServerConnectionString => _sqlServerConnectionString;

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

            var localConnectionString = await SqlServerTestDependency.TryGetLocalComposeConnectionStringAsync();
            if (!string.IsNullOrEmpty(localConnectionString))
            {
                _sqlServerConnectionString = localConnectionString;
                _isInitialized = true;
                return;
            }

            try
            {
                _reuseSqlContainer = SqlServerTestDependency.ShouldReuseContainers();
                var builder = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                    .WithReuse(_reuseSqlContainer);

                if (!_reuseSqlContainer)
                {
                    builder = builder.WithName($"tokenization-sqlserver-{Guid.NewGuid():N}");
                }

                _sqlServerContainer = builder.Build();
                await _sqlServerContainer.StartAsync();
            }
            catch (DockerUnavailableException)
            {
                throw SkipException.ForSkip("Docker is required for integration tests. Start Docker and rerun the integration suite.");
            }

            _sqlServerConnectionString = _sqlServerContainer.GetConnectionString();

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

            if (_sqlServerContainer != null && !_reuseSqlContainer)
            {
                disposeTasks.Add(_sqlServerContainer.StopAsync());
                disposeTasks.Add(_sqlServerContainer.DisposeAsync().AsTask());
            }

            if (disposeTasks.Count > 0)
            {
                await Task.WhenAll(disposeTasks);
            }

            _reuseSqlContainer = false;
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
