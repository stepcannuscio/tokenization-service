using Microsoft.EntityFrameworkCore;
using Tokenization.Domain.Entities;
using Tokenization.Infrastructure.Db.Configurations;
using Tokenization.Infrastructure.Db.Interceptors;

namespace Tokenization.Infrastructure.Db;

/// <summary>
/// EF Core DbContext for token storage.
/// Wires <see cref="TokenRecordConfiguration"/> and applies
/// <see cref="TokenRecordSaveInterceptor"/> to compute blind indexes on save.
/// </summary>
internal sealed class TokensDbContext : DbContext
{
    /// <summary>
    /// Initializes the context with options and the blind-index interceptor.
    /// </summary>
    /// <param name="options">DbContext options.</param>
    public TokensDbContext(DbContextOptions<TokensDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    /// <summary>
    /// Token records (encrypted PAN/PII via <see cref="TokenRecord.EncryptedPayload"/>).
    /// </summary>
    public DbSet<TokenRecord> Tokens => Set<TokenRecord>();

    /// <summary>
    /// Applies entity configurations.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(TokensDbContext).Assembly);
}