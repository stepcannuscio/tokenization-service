using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tokenization.Domain.Entities;
using Tokenization.Infrastructure.Db.Mapping.TokenRecord;
using Tokenization.Infrastructure.Db.Services;
using Tokenization.Tests.Shared.Fixtures;
using Tokenization.Tests.Shared.Utils.ValueObjects;
using Xunit;

namespace Tokenization.Tests.Integration.Db.Services;

/// <summary>
/// Integration tests for BulkOperationsService to ensure proper bulk database operations
/// with real SQL Server database and proper performance characteristics.
/// </summary>
public class BulkOperationsServiceIntegrationTests(SqlServerFixture sqlFixture) : IClassFixture<SqlServerFixture>
{
    private static readonly ILogger<BulkOperationsService> MockLogger =
        new Mock<ILogger<BulkOperationsService>>().Object;

    [Fact]
    public async Task BulkInsertAsync_WithEmptyCollection_ReturnsZero()
    {
        // Arrange
        var dbScope = await sqlFixture.CreateScopeAsync();
        var bulkService = new BulkOperationsService(dbScope.Context, MockLogger);

        // Act
        var result = await bulkService.BulkInsertAsync(new List<TokenRecord>());

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task BulkInsertAsync_WithSingleEntity_InsertsCorrectly()
    {
        // Arrange
        var dbScope = await sqlFixture.CreateScopeAsync();
        var bulkService = new BulkOperationsService(dbScope.Context, MockLogger);
        var entity = TestCreateTokenArgs.Valid("bulk-single").ToTokenRecord(TestEncryptedPayload.Valid());

        // Act
        var result = await bulkService.BulkInsertAsync([entity]);

        // Assert
        result.Should().Be(1);

        var saved = await dbScope.Context.Tokens.AsNoTracking().SingleAsync(t => t.Token == "bulk-single");
        saved.Should().NotBeNull();
        saved.Token.Should().Be("bulk-single");
    }

    [Fact]
    public async Task BulkInsertAsync_WithMultipleEntities_InsertsAllCorrectly()
    {
        // Arrange
        var dbScope = await sqlFixture.CreateScopeAsync();
        var bulkService = new BulkOperationsService(dbScope.Context, MockLogger);

        var entities = Enumerable.Range(1, 5)
            .Select(i => TestCreateTokenArgs.Valid($"bulk-multi-{i}").ToTokenRecord(TestEncryptedPayload.Valid()))
            .ToList();

        // Act
        var result = await bulkService.BulkInsertAsync(entities);

        // Assert
        result.Should().Be(5);

        var savedCount = await dbScope.Context.Tokens
            .AsNoTracking()
            .CountAsync(t => t.Token.StartsWith("bulk-multi-"));
        savedCount.Should().Be(5);
    }

    [Fact]
    public async Task BulkInsertAsync_WithLargeBatch_ProcessesInBatches()
    {
        // Arrange
        var dbScope = await sqlFixture.CreateScopeAsync();
        var bulkService = new BulkOperationsService(dbScope.Context, MockLogger);
        var batchSize = 3; // Small batch size for testing

        var entities = Enumerable.Range(1, 7)
            .Select(i => TestCreateTokenArgs.Valid($"bulk-batch-{i}").ToTokenRecord(TestEncryptedPayload.Valid()))
            .ToList();

        // Act
        var result = await bulkService.BulkInsertAsync(entities, batchSize);

        // Assert
        result.Should().Be(7);

        var savedCount = await dbScope.Context.Tokens
            .AsNoTracking()
            .CountAsync(t => t.Token.StartsWith("bulk-batch-"));
        savedCount.Should().Be(7);
    }

    [Fact]
    public async Task BulkInsertAsync_WithCancellation_RespectsCancellation()
    {
        // Arrange
        var dbScope = await sqlFixture.CreateScopeAsync();
        var bulkService = new BulkOperationsService(dbScope.Context, MockLogger);

        var entities = Enumerable.Range(1, 100)
            .Select(i => TestCreateTokenArgs.Valid($"bulk-cancel-{i}").ToTokenRecord(TestEncryptedPayload.Valid()))
            .ToList();

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            bulkService.BulkInsertAsync(entities, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task BulkUpdateAsync_WithValidExpression_UpdatesCorrectly()
    {
        // Arrange
        var dbScope = await sqlFixture.CreateScopeAsync();
        var bulkService = new BulkOperationsService(dbScope.Context, MockLogger);

        // Insert some test data
        var entities = Enumerable.Range(1, 3)
            .Select(i => TestCreateTokenArgs.Valid($"bulk-update-{i}").ToTokenRecord(TestEncryptedPayload.Valid()))
            .ToList();

        await bulkService.BulkInsertAsync(entities);

        // Act
        var result = await bulkService.BulkUpdateAsync(
            "UPDATE TokenRecords SET IsActive = @p0 WHERE Token LIKE @p1",
            [false, "bulk-update-%"]);

        // Assert
        result.Should().Be(3);

        var inactiveCount = await dbScope.Context.Tokens
            .AsNoTracking()
            .CountAsync(t => t.Token.StartsWith("bulk-update-") && !t.IsActive);
        inactiveCount.Should().Be(3);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithInvalidExpression_ThrowsException()
    {
        // Arrange
        var dbScope = await sqlFixture.CreateScopeAsync();
        var bulkService = new BulkOperationsService(dbScope.Context, MockLogger);

        // Act & Assert
        await Assert.ThrowsAsync<SqlException>(() =>
            bulkService.BulkUpdateAsync("INVALID SQL STATEMENT", []));
    }

    [Fact]
    public async Task BulkUpdateAsync_WithEmptyExpression_ThrowsArgumentException()
    {
        // Arrange
        var dbScope = await sqlFixture.CreateScopeAsync();
        var bulkService = new BulkOperationsService(dbScope.Context, MockLogger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            bulkService.BulkUpdateAsync("", []));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            bulkService.BulkUpdateAsync(null!, []));
    }

    [Fact]
    public async Task BulkDeleteAsync_WithValidExpression_DeletesCorrectly()
    {
        // Arrange
        var dbScope = await sqlFixture.CreateScopeAsync();
        var bulkService = new BulkOperationsService(dbScope.Context, MockLogger);

        // Insert some test data
        var entities = Enumerable.Range(1, 4)
            .Select(i => TestCreateTokenArgs.Valid($"bulk-delete-{i}").ToTokenRecord(TestEncryptedPayload.Valid()))
            .ToList();

        await bulkService.BulkInsertAsync(entities);

        // Act
        var result = await bulkService.BulkDeleteAsync(
            "DELETE FROM TokenRecords WHERE Token LIKE @p0",
            ["bulk-delete-%"]);

        // Assert
        result.Should().Be(4);

        var remainingCount = await dbScope.Context.Tokens
            .AsNoTracking()
            .CountAsync(t => t.Token.StartsWith("bulk-delete-"));
        remainingCount.Should().Be(0);
    }

    [Fact]
    public async Task BulkDeleteAsync_WithInvalidExpression_ThrowsException()
    {
        // Arrange
        var dbScope = await sqlFixture.CreateScopeAsync();
        var bulkService = new BulkOperationsService(dbScope.Context, MockLogger);

        // Act & Assert
        await Assert.ThrowsAsync<SqlException>(() =>
            bulkService.BulkDeleteAsync("INVALID DELETE STATEMENT", []));
    }

    [Fact]
    public async Task BulkDeleteAsync_WithEmptyExpression_ThrowsArgumentException()
    {
        // Arrange
        var dbScope = await sqlFixture.CreateScopeAsync();
        var bulkService = new BulkOperationsService(dbScope.Context, MockLogger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            bulkService.BulkDeleteAsync("", []));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            bulkService.BulkDeleteAsync(null!, []));
    }

    [Fact]
    public async Task BulkOperations_WithTransaction_RespectsTransactionBoundaries()
    {
        // Arrange
        var dbScope = await sqlFixture.CreateScopeAsync();
        var bulkService = new BulkOperationsService(dbScope.Context, MockLogger);

        var entities = Enumerable.Range(1, 3)
            .Select(i => TestCreateTokenArgs.Valid($"transaction-{i}").ToTokenRecord(TestEncryptedPayload.Valid()))
            .ToList();

        // Act - Insert within a transaction and then rollback
        await using var transaction = await dbScope.Context.Database.BeginTransactionAsync();

        var result = await bulkService.BulkInsertAsync(entities);
        result.Should().Be(3);

        // Verify data exists within transaction
        var countInTransaction = await dbScope.Context.Tokens
            .AsNoTracking()
            .CountAsync(t => t.Token.StartsWith("transaction-"));
        countInTransaction.Should().Be(3);

        // Rollback the transaction
        await transaction.RollbackAsync();

        // Assert - Data should not exist after rollback
        var countAfterRollback = await dbScope.Context.Tokens
            .AsNoTracking()
            .CountAsync(t => t.Token.StartsWith("transaction-"));
        countAfterRollback.Should().Be(0);
    }

    [Fact]
    public async Task BulkInsertAsync_WithDuplicateKeys_ThrowsDbUpdateException()
    {
        // Arrange
        var dbScope = await sqlFixture.CreateScopeAsync();
        var bulkService = new BulkOperationsService(dbScope.Context, MockLogger);

        var entity = TestCreateTokenArgs.Valid("duplicate-key").ToTokenRecord(TestEncryptedPayload.Valid());

        // Insert the entity first time
        await bulkService.BulkInsertAsync([entity]);

        // Act & Assert - Try to insert the same entity again
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            bulkService.BulkInsertAsync([entity]));
    }

    [Fact]
    public async Task BulkOperations_WithPerformanceMeasurement_CompletesWithinReasonableTime()
    {
        // Arrange
        var dbScope = await sqlFixture.CreateScopeAsync();
        var bulkService = new BulkOperationsService(dbScope.Context, MockLogger);

        var entities = Enumerable.Range(1, 100)
            .Select(i => TestCreateTokenArgs.Valid($"perf-test-{i}").ToTokenRecord(TestEncryptedPayload.Valid()))
            .ToList();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await bulkService.BulkInsertAsync(entities, batchSize: 50);
        stopwatch.Stop();

        // Assert
        result.Should().Be(100);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds

        var savedCount = await dbScope.Context.Tokens
            .AsNoTracking()
            .CountAsync(t => t.Token.StartsWith("perf-test-"));
        savedCount.Should().Be(100);
    }
}
