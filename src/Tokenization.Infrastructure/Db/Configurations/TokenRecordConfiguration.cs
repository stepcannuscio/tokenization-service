using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tokenization.Domain.Entities;
using Tokenization.Domain.ValueObjects;
using Tokenization.Infrastructure.Db.Constants;

namespace Tokenization.Infrastructure.Db.Configurations;

/// <summary>
/// EF Core mapping for <see cref="TokenRecord"/>:
/// <list type="bullet">
///   <item><description>Defines configuration for token record columns.</description></item>
///   <item><description>Maps <see cref="EncryptedPayload"/> as owned properties for encrypted-at-rest storage.</description></item>
///   <item><description>Adds blind-index shadow columns (HMAC-SHA256) for tenant/customer equality lookups.</description></item>
///   <item><description>Configures indexes that are efficient and PCI-friendly (no indexing ciphertext or raw IDs).</description></item>
/// </list>
/// </summary>
internal sealed class TokenRecordConfiguration : IEntityTypeConfiguration<TokenRecord>
{
    /// <summary>
    /// Configures the <see cref="TokenRecord"/> entity for the current model.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<TokenRecord> builder)
    {
        builder.ToTable("TokenRecords");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        // ------------ Columns ------------
        builder.Property(x => x.Token).HasMaxLength(128).IsRequired();
        builder.Property(x => x.MaskedData).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Last4).HasMaxLength(4).IsRequired();
        builder.Property(x => x.PaymentMethodType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Network).HasMaxLength(32);
        builder.Property(x => x.PaymentMethodMetadata).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.Country).HasMaxLength(2);
        builder.Property(x => x.TenantId).HasMaxLength(128);
        builder.Property(x => x.CustomerId).HasMaxLength(128);
        builder.Property(x => x.InitialTransactionId).HasMaxLength(128);
        builder.Property(x => x.TokenType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.StoredCredentialInitiator).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.StoredCredentialReason).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.MaxUses);
        builder.Property(x => x.UsageCount).HasDefaultValue(0);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnType("datetimeoffset(0)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset(0)").IsRequired();
        builder.Property(x => x.LastUsedAt).HasColumnType("datetimeoffset(0)");
        builder.Property(x => x.ExpiresAt).HasColumnType("datetimeoffset(0)");

        builder.OwnsOne(x => x.EncryptedPayload, ep =>
        {
            ep.Property(p => p.Ciphertext).HasColumnType("varbinary(max)").IsRequired();
            ep.Property(p => p.Nonce).HasColumnType("varbinary(12)").IsRequired();
            ep.Property(p => p.Tag).HasColumnType("varbinary(16)").IsRequired();

            ep.OwnsOne(p => p.WrapPayload, wrap =>
            {
                wrap.Property(w => w.WrappedDek).HasColumnType("varbinary(1024)").IsRequired();
                wrap.Property(w => w.KekKeyId).HasMaxLength(256).IsRequired();
                wrap.Property(w => w.Algorithm).HasMaxLength(64).IsRequired();
                wrap.Property(w => w.WrappedAt).HasColumnType("datetimeoffset(0)").IsRequired();
            });
        });

        // ------------ Shadow properties ------------
        // Blind indexes for fast lookups: WHERE TenantHash = ? AND CustomerHash = ?
        // Use nullable so tokens without tenant/customer references can still be stored
        // Row version for optimistic concurrency
        builder.Property<byte[]>(ShadowProperties.TenantHash).HasColumnType("binary(32)").IsRequired(false);
        builder.Property<byte[]>(ShadowProperties.CustomerHash).HasColumnType("binary(32)").IsRequired(false);
        builder.Property<string>(ShadowProperties.BlindIndexKeyId).HasMaxLength(16);
        builder.Property<byte[]>(ShadowProperties.RowVersion).IsRowVersion();

        // ------------ Indexes ------------
        builder.HasIndex(x => x.Token).IsUnique();
        builder
            .HasIndex(ShadowProperties.TenantHash, ShadowProperties.CustomerHash)
            .HasDatabaseName("IX_token_by_tenant_customer");
        builder
            .HasIndex(ShadowProperties.TenantHash)
            .IncludeProperties(x => new { x.CustomerId, x.Token, x.CreatedAt, x.IsActive });
    }
}
