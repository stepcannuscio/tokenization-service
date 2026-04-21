using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Config.Options;
using Tokenization.Infrastructure.Db.BlindIndex;
using Tokenization.Infrastructure.Db.Config.Options;
using Tokenization.Infrastructure.Db.Enums;
using Tokenization.Infrastructure.Db.Health.Config;
using Tokenization.Infrastructure.Db.Interceptors;
using Tokenization.Infrastructure.Db.Repositories;
using Tokenization.Infrastructure.Db.Services;

namespace Tokenization.Infrastructure.Db.Config;

internal static class DependencyInjection
{
    public static IServiceCollection AddDbInfra(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(config.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IBlindIndexService>(sp =>
        {
            var keyStorageOptions = config.GetSection(KeyStorageOptions.SectionName).Get<KeyStorageOptions>();
            var keyProvider = sp.GetRequiredService<IKeyProvider>();
            return new BlindIndexService(keyProvider, keyStorageOptions?.BlindIndexKeyName ?? string.Empty);
        });

        services.AddScoped<ISaveChangesInterceptor, TokenRecordSaveInterceptor>();
        services.AddScoped<DbCommandInterceptor, PerformanceLoggingInterceptor>();

        services.AddScoped<ITokenRecordRepository, TokenRecordRepository>();
        services.AddScoped<BulkOperationsService>();

        services.AddDbContext<TokensDbContext>((sp, options) =>
            {
                var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

                options.EnableSensitiveDataLogging(false);
                options.EnableDetailedErrors(false);

                switch (dbOptions.Provider)
                {
                    case DatabaseProviderType.SqlServer:
                    {
                        var connectionString = BuildResilientConnectionString(dbOptions);
                        options.UseSqlServer(connectionString, sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(
                                maxRetryCount: dbOptions.MaxRetryCount,
                                maxRetryDelay: TimeSpan.FromSeconds(dbOptions.MaxRetryDelaySeconds),
                                errorNumbersToAdd: null);

                            sqlOptions.CommandTimeout(dbOptions.CommandTimeoutSeconds);
                        });
                        break;
                    }
                    case DatabaseProviderType.Sqlite:
                        options.UseSqlite(dbOptions.ConnectionString);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported database provider: {dbOptions.Provider}");
                }

                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.AddInterceptors(sp.GetServices<DbCommandInterceptor>());
            }
        );

        return services;
    }

    internal static string BuildResilientConnectionString(DatabaseOptions options)
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(options.ConnectionString)
        {
            ConnectTimeout = options.ConnectionTimeoutSeconds,
            MaxPoolSize = options.MaxPoolSize,
            MinPoolSize = options.MinPoolSize,
            LoadBalanceTimeout = options.ConnectionLifetimeSeconds,
            ConnectRetryCount = options.MaxRetryCount,
            ConnectRetryInterval = options.MaxRetryDelaySeconds,
            Encrypt = true,
            MultipleActiveResultSets = false,
            PersistSecurityInfo = false,
            ApplicationName = "TokenizationApi",
            Pooling = true
        };

        if (options.TrustServerCertificate.HasValue)
        {
            connectionStringBuilder.TrustServerCertificate = options.TrustServerCertificate.Value;
        }
        else if (!HasTrustServerCertificateSetting(connectionStringBuilder))
        {
            // Preserve provider defaults when neither config path specifies the setting.
            connectionStringBuilder.Remove("TrustServerCertificate");
        }

        return connectionStringBuilder.ConnectionString;
    }

    private static bool HasTrustServerCertificateSetting(SqlConnectionStringBuilder connectionStringBuilder)
    {
        return connectionStringBuilder.ContainsKey("TrustServerCertificate") ||
               connectionStringBuilder.ContainsKey("Trust Server Certificate");
    }

    internal static async Task InitializeTokenizationDatabaseAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TokensDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
