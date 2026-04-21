namespace Tokenization.Infrastructure.Authorization;

/// <summary>
/// Defines tenant-specific claims for multi-tenant access control.
/// </summary>
public static class TenantClaims
{
    /// <summary>
    /// Canonical claim that indicates the ID of the tenant.
    /// </summary>
    public const string TenantId = "tenant_id";

    /// <summary>
    /// Legacy claim alias accepted for backward compatibility.
    /// </summary>
    public const string MerchantIdAlias = "merchant_id";
}
