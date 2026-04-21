using System.Security.Cryptography;
using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Testcontainers.MsSql;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Db;
using Tokenization.Infrastructure.Db.BlindIndex;
using Tokenization.Infrastructure.Db.Interceptors;
using Xunit;
using Xunit.Sdk;

namespace Tokenization.Tests.Shared.Fixtures;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;
    private bool _reuseContainer;
    private string _connectionStr = string.Empty;
    private string _testDbName = string.Empty;
    private IBlindIndexService? _blindIndexService;

    public async Task InitializeAsync()
    {
        try
        {
            var localConnectionString = await SqlServerTestDependency.TryGetLocalComposeConnectionStringAsync();
            if (!string.IsNullOrEmpty(localConnectionString))
            {
                _connectionStr = localConnectionString;
            }
            else
            {
                _reuseContainer = SqlServerTestDependency.ShouldReuseContainers();
                var builder = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                    .WithReuse(_reuseContainer);

                if (!_reuseContainer)
                {
                    builder = builder.WithName($"tokenization-sqlserver-{Guid.NewGuid():N}");
                }

                _container = builder.Build();
                await _container.StartAsync();
                _connectionStr = _container.GetConnectionString();
            }
        }
        catch (DockerUnavailableException)
        {
            throw SkipException.ForSkip("Docker is required for integration tests. Start Docker and rerun the integration suite.");
        }

        // Create a single test database that will be reused
        _testDbName = $"TestDb_{Guid.NewGuid():N}";
        await using (var conn = new SqlConnection(_connectionStr))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE [{_testDbName}]";
            await cmd.ExecuteNonQueryAsync();
        }

        // Initialize the shared blind index service
        var keyProvider = new Mock<IKeyProvider>();
        var testKey = Convert.FromHexString("00112233445566778899AABBCCDDEEFF00112233445566778899AABBCCDDEEFF");
        keyProvider.Setup(c => c.SignDataAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns<byte[], string, string, CancellationToken>((data, _, keyId, _) =>
            {
                // Simulate HMAC-SHA256 computation using the test key
                using var hmac = new HMACSHA256(testKey);
                return Task.FromResult(hmac.ComputeHash(data));
            });

        _blindIndexService = new BlindIndexService(keyProvider.Object, "blind-index-key");

        // Ensure the database schema exists
        await EnsureDatabaseSchemaAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up the test database
        if (!string.IsNullOrEmpty(_testDbName))
        {
            await using var conn = new SqlConnection(_connectionStr);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                IF DB_ID('{_testDbName}') IS NOT NULL
                BEGIN
                  ALTER DATABASE [{_testDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                  DROP DATABASE [{_testDbName}];
                END
                """;
            await cmd.ExecuteNonQueryAsync();
        }

        if (_container is not null && !_reuseContainer)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    /// <summary>Create a DbContext scope for a test with automatic data cleanup.</summary>
    internal async Task<DbScope> CreateScopeAsync()
    {
        var testConnectionString = new SqlConnectionStringBuilder(_connectionStr)
        {
            InitialCatalog = _testDbName
        }.ConnectionString;

        var indexer = new TokenRecordSaveInterceptor(_blindIndexService!);

        var opts = new DbContextOptionsBuilder<TokensDbContext>()
            .UseSqlServer(testConnectionString)
            .AddInterceptors(indexer)
            .Options;

        var ctx = new TokensDbContext(opts);

        // Ensure database schema is up to date
        await ctx.Database.EnsureCreatedAsync();

        // Clean up any existing data before the test starts
        await CleanupTestDataAsync(ctx);

        return new DbScope(ctx, _blindIndexService!, testConnectionString);
    }

    internal static async Task CleanupTestDataAsync(TokensDbContext context)
    {
        try
        {
            // Remove all test data to ensure test isolation
            // Use raw SQL to ensure complete cleanup
            await context.Database.ExecuteSqlRawAsync("DELETE FROM TokenRecords");
            await context.SaveChangesAsync();
        }
        catch
        {
            // If cleanup fails, continue - this prevents one failing test from breaking others
        }
    }

    private async Task EnsureDatabaseSchemaAsync()
    {
        var testConnectionString = new SqlConnectionStringBuilder(_connectionStr)
        {
            InitialCatalog = _testDbName
        }.ConnectionString;

        var indexer = new TokenRecordSaveInterceptor(_blindIndexService!);

        var opts = new DbContextOptionsBuilder<TokensDbContext>()
            .UseSqlServer(testConnectionString)
            .AddInterceptors(indexer)
            .Options;

        await using var ctx = new TokensDbContext(opts);
        await ctx.Database.EnsureCreatedAsync();
    }
}

internal sealed class DbScope : IAsyncDisposable
{
    public TokensDbContext Context { get; }
    public IBlindIndexService Blind { get; }
    public string ConnectionString { get; }

    internal DbScope(TokensDbContext ctx, IBlindIndexService blind, string connectionString)
        => (Context, Blind, ConnectionString) = (ctx, blind, connectionString);

    public async ValueTask DisposeAsync()
    {
        // Clean up all data from the test database
        await SqlServerFixture.CleanupTestDataAsync(Context);
        await Context.DisposeAsync();
    }
}
