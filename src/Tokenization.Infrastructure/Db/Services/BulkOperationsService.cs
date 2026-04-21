using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Tokenization.Infrastructure.Db.Services;

/// <summary>
/// Service for performing bulk database operations with optimized performance.
/// This service provides efficient bulk insert, update, and delete operations
/// to minimize database round trips and improve performance.
/// </summary>
internal sealed class BulkOperationsService(TokensDbContext dbContext, ILogger<BulkOperationsService> logger)
{
    /// <summary>
    /// Performs bulk insert operation with optimized performance.
    /// </summary>
    /// <param name="entities">Entities to insert.</param>
    /// <param name="batchSize">Number of entities to insert per batch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of entities inserted.</returns>
    /// <exception cref="DbUpdateException">Thrown when the database save fails.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Thrown when the database save fails due to a concurrency violation.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="cancellationToken"/>.</exception>
    public async Task<int> BulkInsertAsync<T>(
        IEnumerable<T> entities,
        int batchSize = 1000,
        CancellationToken cancellationToken = default) where T : class
    {
        var entityList = entities.ToList();
        if (entityList.Count == 0)
        {
            logger.LogDebug("No entities to insert");
            return 0;
        }

        logger.LogInformation("Starting bulk insert of {Count} entities in batches of {BatchSize}", 
            entityList.Count, batchSize);

        var totalInserted = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Process in batches to avoid memory issues and improve performance
            for (var i = 0; i < entityList.Count; i += batchSize)
            {
                var batch = entityList.Skip(i).Take(batchSize).ToList();
                
                await dbContext.Set<T>().AddRangeAsync(batch, cancellationToken);
                var inserted = await dbContext.SaveChangesAsync(cancellationToken);
                totalInserted += inserted;

                logger.LogDebug("Inserted batch {BatchNumber} with {InsertedCount} entities", 
                    i / batchSize + 1, inserted);
            }

            stopwatch.Stop();
            logger.LogInformation("Bulk insert completed: {TotalInserted} entities in {ElapsedMs}ms", 
                totalInserted, stopwatch.ElapsedMilliseconds);

            return totalInserted;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Bulk insert failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Performs bulk update operation using raw SQL for better performance.
    /// </summary>
    /// <param name="updateExpression">SQL update expression with parameters.</param>
    /// <param name="parameters">SQL parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of rows affected.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="cancellationToken"/>.</exception>
    public async Task<int> BulkUpdateAsync(
        string updateExpression,
        object[] parameters,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(updateExpression))
            throw new ArgumentException("Update expression cannot be null or empty", nameof(updateExpression));

        logger.LogInformation("Starting bulk update with expression: {UpdateExpression}", updateExpression);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var affectedRows = await dbContext.Database.ExecuteSqlRawAsync(
                updateExpression, parameters, cancellationToken);

            stopwatch.Stop();
            logger.LogInformation("Bulk update completed: {AffectedRows} rows affected in {ElapsedMs}ms", 
                affectedRows, stopwatch.ElapsedMilliseconds);

            return affectedRows;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Bulk update failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Performs bulk delete operation using raw SQL for better performance.
    /// </summary>
    /// <param name="deleteExpression">SQL delete expression with parameters.</param>
    /// <param name="parameters">SQL parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of rows affected.</returns>
    public async Task<int> BulkDeleteAsync(
        string deleteExpression,
        object[] parameters,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deleteExpression))
            throw new ArgumentException("Delete expression cannot be null or empty", nameof(deleteExpression));

        logger.LogInformation("Starting bulk delete with expression: {DeleteExpression}", deleteExpression);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var affectedRows = await dbContext.Database.ExecuteSqlRawAsync(
                deleteExpression, parameters, cancellationToken);

            stopwatch.Stop();
            logger.LogInformation("Bulk delete completed: {AffectedRows} rows affected in {ElapsedMs}ms", 
                affectedRows, stopwatch.ElapsedMilliseconds);

            return affectedRows;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Bulk delete failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
