using Microsoft.EntityFrameworkCore;
using Tokenization.Domain.Abstractions;
using Tokenization.Domain.ValueObjects;
using Tokenization.Infrastructure.Db.BlindIndex;
using Tokenization.Infrastructure.Db.Constants;
using Tokenization.Infrastructure.Db.Mapping.TokenRecord;
using Tokenization.Infrastructure.Db.Services;

namespace Tokenization.Infrastructure.Db.Repositories;

/// <summary>
/// EF Core-backed repository that:
/// <list type="bullet">
///   <item><description>Encrypts PCI payloads with <see cref="IEncryptionService"/> before persisting.</description></item>
///   <item><description>Queries by tenant/customer via blind indexes (no plaintext in indexes).</description></item>
///   <item><description>Exposes only non-sensitive summaries unless explicit decryption is requested.</description></item>
/// </list>
/// This class must be used only within the PCI-scoped application boundary.
/// </summary>
internal sealed class TokenRecordRepository(
    TokensDbContext db,
    IBlindIndexService blind,
    BulkOperationsService bulkSvc) : ITokenRecordRepository
{
    /// <inheritdoc />
    /// <exception cref="DbUpdateException">Thrown when the database save fails.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Thrown when the database save fails due to a concurrency violation.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="ct"/>.</exception>
    public async Task<TokenSummary> CreateAsync(CreateTokenArgs args, EncryptedPayload encryptedPayload,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(encryptedPayload);

        var entity = args.ToTokenRecord(encryptedPayload);
        db.Tokens.Add(entity);
        await db.SaveChangesAsync(ct);

        return entity.ToSummary();
    }

    /// <inheritdoc />
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="ct"/>.</exception>
    public async Task<TokenSummary?> GetSummaryByTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token is required.", nameof(token));

        return await db.Tokens
            .AsNoTracking()
            .Where(t => t.Token == token)
            .Select(t => t.ToSummary())
            .SingleOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="ct"/>.</exception>
    public async Task<IReadOnlyList<TokenSummary>> FindByTenantCustomerAsync(string tenantId, string customerId,
        int take = 50, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("CustomerId is required.", nameof(customerId));
        if (take < 1) throw new ArgumentException("Take must be at least 1.", nameof(take));

        var tenantIndex = await blind.ComputeAsync(tenantId, null, ct);
        var customerIndex = await blind.ComputeAsync(customerId, null, ct);

        return await db.Tokens
            .AsNoTracking()
            .Where(t =>
                EF.Property<byte[]>(t, ShadowProperties.TenantHash) == tenantIndex &&
                EF.Property<byte[]>(t, ShadowProperties.CustomerHash) == customerIndex)
            .OrderByDescending(t => t.CreatedAt)
            .Take(Math.Max(1, take))
            .Select(t => t.ToSummary())
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="ct"/>.</exception>
    public async Task<EncryptedPayload?> GetEncryptedPayloadAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token is required.", nameof(token));

        return await db.Tokens
            .AsNoTracking()
            .Where(t => t.Token == token)
            .Select(t => t.EncryptedPayload)
            .SingleOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    /// <exception cref="DbUpdateException">Thrown when the database save fails.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Thrown when the database save fails due to a concurrency violation.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="ct"/>.</exception>
    public async Task<TokenUsageResult> IncrementUsageAsync(string token, DateTimeOffset nowUtc,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token is required.", nameof(token));

        var entity = await db.Tokens.SingleOrDefaultAsync(t => t.Token == token, ct);
        if (entity is null) throw new InvalidOperationException($"Token '{token}' not found.");

        entity.UsageCount += 1;
        entity.LastUsedAt = nowUtc;

        await db.SaveChangesAsync(ct);

        return entity.ToUsageResult();
    }

    /// <inheritdoc />
    /// <exception cref="DbUpdateException">Thrown when the database save fails.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Thrown when the database save fails due to a concurrency violation.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="ct"/>.</exception>
    public async Task<bool> DeactivateAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token is required.", nameof(token));

        var entity = await db.Tokens.SingleOrDefaultAsync(t => t.Token == token, ct);
        if (entity is null) return false;

        if (!entity.IsActive) return true;

        entity.IsActive = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <inheritdoc />
    /// <exception cref="DbUpdateException">Thrown when the database save fails.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Thrown when the database save fails due to a concurrency violation.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="ct"/>.</exception>
    public async Task<bool> DeleteAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token is required.", nameof(token));

        var entity = await db.Tokens.SingleOrDefaultAsync(t => t.Token == token, ct);
        if (entity is null) return false;

        db.Tokens.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <inheritdoc />
    /// <exception cref="DbUpdateException">Thrown when the database save fails.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Thrown when the database save fails due to a concurrency violation.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="ct"/>.</exception>
    public async Task<IReadOnlyList<TokenSummary>> BulkCreateAsync(
        IEnumerable<(CreateTokenArgs args, EncryptedPayload payload)> tokenData,
        CancellationToken ct = default)
    {
        var dataList = tokenData.ToList();
        if (dataList.Count == 0)
        {
            return [];
        }

        var entities = dataList.Select(item => item.args.ToTokenRecord(item.payload)).ToList();

        await bulkSvc.BulkInsertAsync(entities, cancellationToken: ct);

        return entities.Select(e => e.ToSummary()).ToList();
    }

    /// <inheritdoc />
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="ct"/>.</exception>
    public async Task<int> BulkDeactivateAsync(
        IEnumerable<string> tokens,
        CancellationToken ct = default)
    {
        var tokenList = tokens.ToList();
        if (tokenList.Count == 0)
        {
            return 0;
        }

        const string sql = """
                               UPDATE TokenRecords 
                               SET IsActive = 0, UpdatedAt = GETDATE()
                               WHERE Token IN ({0}) AND IsActive = 1
                           """;

        var parameters = tokenList.Cast<object>().ToArray();
        var allParameters = parameters.Concat([DateTimeOffset.UtcNow]).ToArray();

        return await bulkSvc.BulkUpdateAsync(
            string.Format(sql, string.Join(",", parameters.Select((_, i) => $"@p{i}"))),
            allParameters, ct);
    }
}
