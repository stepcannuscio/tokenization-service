namespace Tokenization.Infrastructure.Db.Constants;

/// <summary>
/// Default shadow properties added to database tables.
/// </summary>
internal static class ShadowProperties
{
    public const string CustomerHash = "CustomerHash";

    public const string TenantHash = "TenantHash";
    
    public const string BlindIndexKeyId = "IndexKeyId";

    public const string RowVersion = "RowVersion";
}