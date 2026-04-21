using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tokenization.Tests.Shared.Utils.Authentication;
using Xunit;
using Xunit.Sdk;

namespace Tokenization.Tests.Shared.Fixtures;

/// <summary>
/// Full integration test factory using shared SQL Server and Redis Cache containers.
/// </summary>
public class WebApplicationFactoryFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IntegrationTestCollectionFixture _collectionFixture;
    private readonly SqlServerFixture _sqlServerFixture;
    private DbScope? _dbScope;
    private string _dbConnectionString = string.Empty;
    private string _redisConnectionString = string.Empty;

    public WebApplicationFactoryFixture(IntegrationTestCollectionFixture collectionFixture)
    {
        _collectionFixture = collectionFixture;
        _sqlServerFixture = new SqlServerFixture();
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Initialize the shared SQL Server fixture
            await _sqlServerFixture.InitializeAsync();

            // Create a test database
            _dbScope = await _sqlServerFixture.CreateScopeAsync();
            _dbConnectionString = _dbScope.Context.Database.GetConnectionString() ?? string.Empty;

            // Configure redis connection from shared fixture
            _redisConnectionString = _collectionFixture.RedisConnectionString;
        }
        catch (Exception ex)
        {
            if (ex is SkipException)
            {
                throw;
            }

            await CleanupAsync();
            throw new InvalidOperationException("Failed to initialize test infrastructure", ex);
        }
    }

    private async Task CleanupAsync()
    {
        try
        {
            await _sqlServerFixture.DisposeAsync();
            // Note: Redis container is managed by the collection fixture, not disposed here
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    public new async Task DisposeAsync()
    {
        await CleanupAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Minimal configuration for full integration tests
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                // Cache configuration - use Redis for integration tests
                ["Cache:RedisConnectionString"] = _redisConnectionString,

                // // Key storage configuration
                ["KeyStorage:KeyProvider"] = "InMemory",

                // Database configuration - use SQL Server for integration tests
                ["Database:ConnectionString"] = _dbConnectionString,

                // Disable Swagger for tests
                ["Swagger:Enabled"] = "false",
            };
            config.AddInMemoryCollection(testConfig);
        });

        // Add test infrastructure
        builder.ConfigureServices(services =>
        {
            // Add SQL Server TokensDb context
            if (_dbScope != null) services.AddSingleton(_dbScope.Context);

            // Replace authentication with test authentication
            services.RemoveAll<IAuthenticationSchemeProvider>();
            services.RemoveAll<IAuthenticationHandlerProvider>();
            services.RemoveAll<IAuthenticationService>();

            services.AddAuthentication("Test")
                .AddScheme<TestAuthenticationSchemeOptions, TestAuthorizationHandler>("Test", options =>
                {
                    // Use a unique user ID for each test to avoid rate limiting conflicts
                    options.DefaultUserId = $"test-user-{Guid.NewGuid():N}";
                    options.DefaultTenantId = "test-tenant-456";
                });
        });

        // Minimal logging for tests
        builder.ConfigureLogging(logging => { logging.ClearProviders(); });
    }
}
