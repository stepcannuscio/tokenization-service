namespace Tokenization.Infrastructure.Authorization;

/// <summary>
/// Types of tenant validation that can be performed.
/// </summary>
public enum TenantValidationType
{
    /// <summary>
    /// Validate tenant access based on a tenant ID in the request body.
    /// </summary>
    RequestBody,

    /// <summary>
    /// Validate tenant access based on a tenant ID in route parameters.
    /// </summary>
    RouteParameter,

    /// <summary>
    /// Validate access based on the current tenant context only.
    /// </summary>
    CurrentContext
}
