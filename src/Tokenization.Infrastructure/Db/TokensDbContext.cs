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
    public TokensDbContext(DbContextOptions<TokensDbContext> options) : base(options)
    {
    }

    public DbSet<TokenRecord> Tokens => Set<TokenRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(TokensDbContext).Assembly);
}
