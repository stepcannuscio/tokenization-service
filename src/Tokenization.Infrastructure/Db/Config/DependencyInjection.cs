using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Config.Options;
using Tokenization.Infrastructure.Db.BlindIndex;
using Tokenization.Infrastructure.Db.Config.Options;
using Tokenization.Infrastructure.Db.Health.Config;
using Tokenization.Infrastructure.Db.Interceptors;
using Tokenization.Infrastructure.Db.Repositories;
using Tokenization.Infrastructure.Db.Services;

namespace Tokenization.Infrastructure.Db.Config;

/// <summary>
/// DI registration for the database infrastructure.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Adds the database context to the service collection.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="config">Configuration root used to bind <see cref="DatabaseOptions"/></param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddDbInfra(this IServiceCollection services, IConfiguration config)
    {
        // Configure database options with validation
        services.AddOptions<DatabaseOptions>()
            .Bind(config.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register database services
        services.AddScoped<IBlindIndexService>(sp =>
        {
            var keyStorageOptions = config.GetSection(KeyStorageOptions.SectionName).Get<KeyStorageOptions>();
            var keyProvider = sp.GetRequiredService<IKeyProvider>();
            return new BlindIndexService(keyProvider, keyStorageOptions?.BlindIndexKeyName ?? string.Empty);
        });

        // Register interceptors
        services.AddScoped<ISaveChangesInterceptor, TokenRecordSaveInterceptor>();
        services.AddScoped<DbCommandInterceptor, PerformanceLoggingInterceptor>();

        // Register repositories and services
        services.AddScoped<ITokenRecordRepository,TokenRecordRepository>();
        services.AddScoped<BulkOperationsService>();

        // Get database options for configuration
        var dbOptions = config.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() 
                       ?? throw new InvalidOperationException("Database configuration is required");

        // Build resilient connection string
        var connectionString = BuildResilientConnectionString(dbOptions);

        // Configure EF Core with comprehensive resilience features
        services.AddDbContext<TokensDbContext>((sp, options) =>
            {
                // Security settings
                options.EnableSensitiveDataLogging(false);
                options.EnableDetailedErrors(false);

                // Configure SQL Server with comprehensive resilience
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    // Enhanced retry policy with exponential backoff
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: dbOptions.MaxRetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(dbOptions.MaxRetryDelaySeconds),
                        errorNumbersToAdd: null);

                    // Command timeout
                    sqlOptions.CommandTimeout(dbOptions.CommandTimeoutSeconds);
                });

                // Add all interceptors
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.AddInterceptors(sp.GetServices<DbCommandInterceptor>());
            }
        );

        return services;
    }

    /// <summary>
    /// Builds a resilient connection string with comprehensive resilience parameters.
    /// </summary>
    /// <param name="options">Database options.</param>
    /// <returns>Resilient connection string.</returns>
    private static string BuildResilientConnectionString(DatabaseOptions options)
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(options.ConnectionString)
        {
            // Connection timeout settings
            ConnectTimeout = options.ConnectionTimeoutSeconds,
            // Connection pooling settings
            MaxPoolSize = options.MaxPoolSize,
            MinPoolSize = options.MinPoolSize,
            LoadBalanceTimeout = options.ConnectionLifetimeSeconds,
            // Resilience settings
            ConnectRetryCount = options.MaxRetryCount,
            ConnectRetryInterval = options.MaxRetryDelaySeconds,
            // Security settings
            Encrypt = true,
            TrustServerCertificate = options.TrustServerCertificate,
            MultipleActiveResultSets = false,
            PersistSecurityInfo = false,
            // Performance settings
            ApplicationName = "TokenizationApi",
            Pooling = true
        };

        return connectionStringBuilder.ConnectionString;
    }
}