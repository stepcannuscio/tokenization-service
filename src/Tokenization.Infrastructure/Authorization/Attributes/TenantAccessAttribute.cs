using Tokenization.Infrastructure.Authorization.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Tokenization.Infrastructure.Authorization.Attributes;

/// <summary>
/// Attribute that applies tenant authorization to controllers and actions.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class TenantAccessAttribute : TypeFilterAttribute
{
    private const TenantValidationType DefaultValidationType = TenantValidationType.CurrentContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAccessAttribute"/> class.
    /// </summary>
    public TenantAccessAttribute() : this(DefaultValidationType)
    {
        ValidationType = DefaultValidationType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantAccessAttribute"/> class with the specified validation type.
    /// </summary>
    /// <param name="validationType">The type of tenant validation to perform.</param>
    public TenantAccessAttribute(TenantValidationType validationType) : base(typeof(TenantAuthorizationFilter))
    {
        ValidationType = validationType;
    }

    public TenantValidationType ValidationType { get; }
}
