using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tokenization.Infrastructure.Db.Config.Options;

namespace Tokenization.Infrastructure.Db.Interceptors;

/// <summary>
/// EF Core interceptor that logs slow database operations for performance monitoring.
/// This interceptor helps identify performance bottlenecks and optimize database queries.
/// </summary>
internal sealed class PerformanceLoggingInterceptor(
    ILogger<PerformanceLoggingInterceptor> logger,
    IOptions<DatabaseOptions> options)
    : DbCommandInterceptor
{
    private readonly DatabaseOptions _options = options.Value;

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnablePerformanceLogging)
            return result;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var readerResult = await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > _options.SlowQueryThresholdMs)
            {
                logger.LogWarning(
                    "Slow query detected: {CommandText} executed in {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                    TruncateCommandText(command.CommandText),
                    stopwatch.ElapsedMilliseconds,
                    _options.SlowQueryThresholdMs);
            }

            return readerResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Query execution failed after {ElapsedMs}ms: {CommandText}",
                stopwatch.ElapsedMilliseconds,
                TruncateCommandText(command.CommandText));
            throw;
        }
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnablePerformanceLogging)
            return result;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > _options.SlowQueryThresholdMs)
            {
                logger.LogWarning(
                    "Slow non-query detected: {CommandText} executed in {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                    TruncateCommandText(command.CommandText),
                    stopwatch.ElapsedMilliseconds,
                    _options.SlowQueryThresholdMs);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Non-query execution failed after {ElapsedMs}ms: {CommandText}",
                stopwatch.ElapsedMilliseconds,
                TruncateCommandText(command.CommandText));
            throw;
        }
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnablePerformanceLogging)
            return ValueTask.FromResult(result);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > _options.SlowQueryThresholdMs)
            {
                logger.LogWarning(
                    "Slow scalar query detected: {CommandText} executed in {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                    TruncateCommandText(command.CommandText),
                    stopwatch.ElapsedMilliseconds,
                    _options.SlowQueryThresholdMs);
            }

            return ValueTask.FromResult(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Scalar query execution failed after {ElapsedMs}ms: {CommandText}",
                stopwatch.ElapsedMilliseconds,
                TruncateCommandText(command.CommandText));
            throw;
        }
    }

    /// <summary>
    /// Truncates long queries.
    /// </summary>
    private static string TruncateCommandText(string commandText)
    {
        if (string.IsNullOrEmpty(commandText))
            return string.Empty;

        if (commandText.Length > 200)
            commandText = commandText[..200] + "...";

        return commandText;
    }
}
