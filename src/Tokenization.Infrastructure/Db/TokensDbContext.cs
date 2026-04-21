using Microsoft.EntityFrameworkCore;
using Tokenization.Domain.Entities;
using Tokenization.Infrastructure.Db.Configurations;
using Tokenization.Infrastructure.Db.Constants;
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
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TokensDbContext).Assembly);

        if (Database.IsSqlite())
        {
            ConfigureSqliteModel(modelBuilder);
        }
    }

    private static void ConfigureSqliteModel(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<TokenRecord>();

        builder.Property(x => x.PaymentMethodMetadata).HasColumnType("TEXT");
        builder.Property(x => x.CreatedAt).HasColumnType("TEXT");
        builder.Property(x => x.UpdatedAt).HasColumnType("TEXT");
        builder.Property(x => x.LastUsedAt).HasColumnType("TEXT");
        builder.Property(x => x.ExpiresAt).HasColumnType("TEXT");

        builder.OwnsOne(x => x.EncryptedPayload, ep =>
        {
            ep.Property(p => p.Ciphertext).HasColumnType("BLOB");
            ep.Property(p => p.Nonce).HasColumnType("BLOB");
            ep.Property(p => p.Tag).HasColumnType("BLOB");

            ep.OwnsOne(p => p.WrapPayload, wrap =>
            {
                wrap.Property(w => w.WrappedDek).HasColumnType("BLOB");
                wrap.Property(w => w.WrappedAt).HasColumnType("TEXT");
            });
        });

        builder.Property<byte[]>(ShadowProperties.TenantHash).HasColumnType("BLOB").IsRequired(false);
        builder.Property<byte[]>(ShadowProperties.CustomerHash).HasColumnType("BLOB").IsRequired(false);
        builder.Property<byte[]>(ShadowProperties.RowVersion).HasColumnType("BLOB").IsRowVersion();
    }
}
