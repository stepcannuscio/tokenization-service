using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Authorization.Attributes;

namespace Tokenization.Infrastructure.Authorization.Filters;

/// <summary>
/// Action filter that validates tenant access for multi-tenant scenarios.
/// </summary>
public class TenantAuthorizationFilter(
    ITenantContextService tenantContext,
    ILogger<TenantAuthorizationFilter> logger)
    : IAsyncActionFilter
{
    private readonly ITenantContextService _tenantContext =
        tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));

    private readonly ILogger<TenantAuthorizationFilter> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("Tenant authorization failed: User not authenticated. Request: {Method} {Path}",
                context.HttpContext.Request.Method, context.HttpContext.Request.Path);

            context.Result = new UnauthorizedResult();
            return;
        }

        try
        {
            var currentTenantId = _tenantContext.TryGetCurrentTenantId();
            if (string.IsNullOrEmpty(currentTenantId))
            {
                _logger.LogWarning(
                    "Tenant authorization failed: No tenant context found. User: {UserId}, Request: {Method} {Path}",
                    GetUserId(context.HttpContext.User), context.HttpContext.Request.Method, context.HttpContext.Request.Path);

                context.Result = new ForbidResult();
                return;
            }

            var validationType = GetValidationType(context);
            var isValid = validationType switch
            {
                TenantValidationType.RequestBody => await ValidateRequestBodyAccess(context, currentTenantId),
                TenantValidationType.RouteParameter => ValidateRouteParameterAccess(context, currentTenantId),
                TenantValidationType.CurrentContext => true,
                _ => false
            };

            if (!isValid)
            {
                _logger.LogWarning(
                    "Tenant authorization failed: Access denied for tenant {TenantId}. User: {UserId}, Request: {Method} {Path}",
                    currentTenantId, GetUserId(context.HttpContext.User), context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path);

                context.Result = new ForbidResult();
                return;
            }

            await next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tenant authorization error. User: {UserId}, Request: {Method} {Path}",
                GetUserId(context.HttpContext.User), context.HttpContext.Request.Method, context.HttpContext.Request.Path);

            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    private static TenantValidationType GetValidationType(ActionExecutingContext context)
    {
        var controllerAttribute = context.Controller.GetType()
            .GetCustomAttributes(typeof(TenantAccessAttribute), true)
            .OfType<TenantAccessAttribute>()
            .FirstOrDefault();

        if (controllerAttribute is not null)
        {
            return controllerAttribute.ValidationType;
        }

        var actionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
        var actionAttribute = actionDescriptor.MethodInfo
            .GetCustomAttributes(typeof(TenantAccessAttribute), true)
            .OfType<TenantAccessAttribute>()
            .FirstOrDefault();

        return actionAttribute?.ValidationType ?? TenantValidationType.CurrentContext;
    }

    private static async Task<bool> ValidateRequestBodyAccess(ActionExecutingContext context, string currentTenantId)
    {
        try
        {
            var requestBody = await ReadRequestBodyAsync(context.HttpContext.Request);
            using var doc = JsonDocument.Parse(requestBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("TenantId", out var tenantId) ||
                root.TryGetProperty("tenantId", out tenantId) ||
                root.TryGetProperty("tenant_id", out tenantId) ||
                root.TryGetProperty("tenantId", out tenantId) ||
                root.TryGetProperty("tenant_id", out tenantId))
            {
                return string.Equals(tenantId.GetString(), currentTenantId, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateRouteParameterAccess(ActionExecutingContext context, string currentTenantId)
    {
        var routeValues = context.RouteData.Values;
        var tenantIdInRoute = routeValues.GetValueOrDefault("TenantId")?.ToString() ??
                              routeValues.GetValueOrDefault("tenantId")?.ToString() ??
                              routeValues.GetValueOrDefault("tenant_id")?.ToString() ??
                              routeValues.GetValueOrDefault("tenantId")?.ToString() ??
                              routeValues.GetValueOrDefault("tenant_id")?.ToString();

        return tenantIdInRoute is null ||
               string.Equals(tenantIdInRoute, currentTenantId, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               user.FindFirst("sub")?.Value ??
               user.Identity?.Name ??
               "unknown";
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        try
        {
            request.EnableBuffering();
            request.Body.Position = 0;

            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            return body;
        }
        catch
        {
            return string.Empty;
        }
    }
}
