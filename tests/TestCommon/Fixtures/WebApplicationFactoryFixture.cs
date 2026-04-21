using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Tokenization.Tests.Shared.Utils.Authentication;
using Xunit;

namespace Tokenization.Tests.Shared.Fixtures;

/// <summary>
/// Full integration test factory using an in-memory SQLite database for fast API host coverage.
/// </summary>
public class WebApplicationFactoryFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
    private SqliteConnection? _connection;
    private string _dbConnectionString = string.Empty;
    private bool _isInitialized;

    public async Task InitializeAsync()
    {
        await _initializationSemaphore.WaitAsync();
        try
        {
            if (_isInitialized)
            {
                return;
            }

            _connection = new SqliteConnection($"Data Source=file:tokenization-api-tests-{Guid.NewGuid():N}?mode=memory&cache=shared");
            await _connection.OpenAsync();
            _dbConnectionString = _connection.ConnectionString;
            _isInitialized = true;
        }
        catch
        {
            await CleanupAsync();
            throw;
        }
        finally
        {
            _initializationSemaphore.Release();
        }
    }

    private async Task CleanupAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _dbConnectionString = string.Empty;
        _isInitialized = false;
    }

    public new async Task DisposeAsync()
    {
        await CleanupAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        EnsureInitialized();
        builder.UseEnvironment("Development");

        // Minimal configuration for full integration tests
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                // Cache configuration - keep API integration host in-memory for determinism
                ["Cache:RedisConnectionString"] = string.Empty,
                ["Cache:InstanceName"] = string.Empty,
                ["Cache:EnableHealthChecks"] = "false",

                // Key storage configuration
                ["KeyStorage:KeyProvider"] = "InMemory",
                ["KeyStorage:EnableHealthChecks"] = "false",

                // Database configuration - use an in-memory SQLite database for fast API host tests
                ["Database:Provider"] = "Sqlite",
                ["Database:ConnectionString"] = _dbConnectionString,
                ["Database:TrustServerCertificate"] = "true",
                ["Database:EnableHealthChecks"] = "false",

                // Tighter timeouts so test failures surface quickly instead of waiting 45+ seconds
                ["Database:MaxRetryCount"] = "1",
                ["Database:MaxRetryDelaySeconds"] = "5",
                ["Database:ConnectionTimeoutSeconds"] = "5",
                ["Database:CommandTimeoutSeconds"] = "10",

                // Disable HTTPS redirects in the in-memory test host
                ["Security:UseHttpsRedirection"] = "false",

                // Disable Swagger for tests
                ["Swagger:Enabled"] = "false",
            };
            config.AddInMemoryCollection(testConfig);
        });

        // Add test infrastructure
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
            services.PostConfigure<HealthCheckServiceOptions>(options =>
            {
                var registrationsToRemove = options.Registrations
                    .Where(registration => registration.Name != "api")
                    .ToList();

                foreach (var registration in registrationsToRemove)
                {
                    options.Registrations.Remove(registration);
                }
            });

            services.AddTestAuthentication("Test", options =>
            {
                // Use a unique user ID for each test to avoid rate limiting conflicts
                options.DefaultUserId = $"test-user-{Guid.NewGuid():N}";
                options.DefaultTenantId = "test-tenant-456";
            });
        });

        // Minimal logging for tests
        builder.ConfigureLogging(logging => { logging.ClearProviders(); });
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        InitializeAsync().GetAwaiter().GetResult();
    }
}
