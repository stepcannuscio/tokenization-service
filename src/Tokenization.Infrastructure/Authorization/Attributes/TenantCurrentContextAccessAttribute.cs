namespace Tokenization.Infrastructure.Authorization.Attributes;

/// <summary>
/// Attribute that applies tenant authorization with current-context validation.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class TenantCurrentContextAccessAttribute()
    : TenantAccessAttribute(TenantValidationType.CurrentContext);
