using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Config.Options;
using Tokenization.Infrastructure.Crypto.Enums;
using Tokenization.Infrastructure.Db;
using Tokenization.Infrastructure.Db.Config;
using Tokenization.Infrastructure.Db.Config.Options;
using Xunit;

namespace Tokenization.UnitTests.Infrastructure.Db.Config;

public class DependencyInjectionTests
{
    [Fact]
    public void BuildResilientConnectionString_Preserves_TrustServerCertificate_From_ConnectionString_When_Override_Is_Unset()
    {
        var options = CreateSqlServerOptions(
            "Server=localhost,14333;Database=TokenizationService;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True");

        var connectionString = DependencyInjection.BuildResilientConnectionString(options);

        new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString).TrustServerCertificate.Should().BeTrue();
    }

    [Fact]
    public void BuildResilientConnectionString_Uses_Explicit_False_Override()
    {
        var options = CreateSqlServerOptions(
            "Server=localhost,14333;Database=TokenizationService;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True");
        options.TrustServerCertificate = false;

        var connectionString = DependencyInjection.BuildResilientConnectionString(options);

        new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString).TrustServerCertificate.Should().BeFalse();
    }

    [Fact]
    public void BuildResilientConnectionString_Uses_Explicit_True_Override_When_ConnectionString_Omits_The_Setting()
    {
        var options = CreateSqlServerOptions(
            "Server=localhost,14333;Database=TokenizationService;User Id=sa;Password=Your_strong_password123");
        options.TrustServerCertificate = true;

        var connectionString = DependencyInjection.BuildResilientConnectionString(options);

        new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString).TrustServerCertificate.Should().BeTrue();
    }

    [Fact]
    public void AddDbInfra_Configures_DbContext_With_ConnectionString_TrustServerCertificate_When_Override_Is_Unset()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] =
                    "Server=localhost,14333;Database=TokenizationService;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True",
                ["KeyStorage:KeyProvider"] = KeyProviderType.InMemory.ToString(),
                ["KeyStorage:VaultUrl"] = "https://localhost.localdomain/",
                ["KeyStorage:KekKeyName"] = "kek-main",
                ["KeyStorage:BlindIndexKeyName"] = "blind-index-main",
                ["KeyStorage:UseInMemoryKeys"] = "true"
            })
            .Build();

        services.AddSingleton(Mock.Of<IKeyProvider>());
        services.AddLogging();
        services.AddOptions();
        services.AddDbInfra(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<TokensDbContext>();
        var connectionString = dbContext.Database.GetConnectionString();

        connectionString.Should().NotBeNullOrWhiteSpace();
        new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString).TrustServerCertificate.Should().BeTrue();
    }

    private static DatabaseOptions CreateSqlServerOptions(string connectionString)
    {
        return new DatabaseOptions
        {
            ConnectionString = connectionString,
            MaxRetryCount = 3,
            MaxRetryDelaySeconds = 30,
            BaseDelaySeconds = 2,
            CommandTimeoutSeconds = 30,
            ConnectionTimeoutSeconds = 15,
            MaxPoolSize = 100,
            MinPoolSize = 5,
            ConnectionLifetimeSeconds = 300,
            EnableHealthChecks = true,
            HealthCheckTimeoutSeconds = 5,
            EnablePerformanceLogging = true,
            SlowQueryThresholdMs = 1000
        };
    }
}
