using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tokenization.Domain.Entities;
using Tokenization.Infrastructure.Db.BlindIndex;
using Tokenization.Infrastructure.Db.Constants;

namespace Tokenization.Infrastructure.Db.Interceptors;

/// <summary>
/// EF Core interceptor that populates/refreshes blind-index shadow columns and timestamps
/// for <see cref="TokenRecord"/> on SaveChanges. This keeps PCI-sensitive fields
/// out of indexes while preserving fast equality lookups.
/// </summary>
internal sealed class TokenRecordSaveInterceptor : SaveChangesInterceptor
{
    private readonly IBlindIndexService _blind;

    /// <summary>
    /// Creates an interceptor that uses <see cref="IBlindIndexService"/> to compute hashes.
    /// </summary>
    /// <param name="blind">Blind-index service used to derive <c>TenantHash</c>/<c>CustomerHash</c>.</param>
    public TokenRecordSaveInterceptor(IBlindIndexService blind) => _blind = blind;

    /// <summary>
    /// Populates shadow properties <c>TenantHash</c>, <c>CustomerHash</c>, and ensures UTC timestamps
    /// prior to saving <see cref="TokenRecord"/> entities.
    /// </summary>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await ApplyIndexer(eventData);
        UpdateTimestamps(eventData);
        return result;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyIndexer(eventData).GetAwaiter().GetResult();
        UpdateTimestamps(eventData);
        return result;
    }

    private async Task ApplyIndexer(DbContextEventData eventData)
    {
        if (eventData.Context is null) return;

        foreach (var entry in eventData.Context.ChangeTracker.Entries<TokenRecord>())
        {
            if (entry.Entity.GetType() != typeof(TokenRecord)) continue;
            
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;

            // Blind-index rotation key id
            var keyId = (string?)entry.Property(ShadowProperties.BlindIndexKeyId).CurrentValue;

            // Compute hashes from plaintext IDs (do not index plaintext columns)
            entry.Property(ShadowProperties.TenantHash).CurrentValue =
                await _blind.ComputeAsync(entry.Entity.TenantId, keyId);
         
            entry.Property(ShadowProperties.CustomerHash).CurrentValue =
                await _blind.ComputeAsync(entry.Entity.CustomerId, keyId);
        }
    }
    
    private void UpdateTimestamps(DbContextEventData eventData)
    {
        if (eventData.Context is null) return;

        foreach (var entry in eventData.Context.ChangeTracker.Entries<TokenRecord>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;

            var now = DateTimeOffset.UtcNow;
            
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = now;
            }

            entry.Entity.UpdatedAt = now;
        }
    }
}