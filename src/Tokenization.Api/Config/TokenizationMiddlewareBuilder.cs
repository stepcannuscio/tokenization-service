using Tokenization.Api.Config.Options;
using Tokenization.Api.Controllers.Config;
using Tokenization.Api.ExceptionHandling.Config;
using Tokenization.Api.Idempotency.Config;
using Tokenization.Api.Logging.Config;
using Tokenization.Api.OpenApi.Config;
using Tokenization.Api.Security.Config;

namespace Tokenization.Api.Config;

/// <summary>
/// Builder for configuring Tokenization API middleware pipeline.
/// Provides a fluent interface for configuring middleware in the correct order.
/// </summary>
internal sealed class TokenizationMiddlewareBuilder(WebApplication app, IConfiguration configuration)
{
    /// <summary>
    /// Configures exception handling middleware (should be first).
    /// </summary>
    public TokenizationMiddlewareBuilder UseExceptionHandling()
    {
        app.UseTokenizationExceptionHandling();
        return this;
    }

    /// <summary>
    /// Configures production-specific middleware (HSTS).
    /// </summary>
    public TokenizationMiddlewareBuilder UseHsts()
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }
        return this;
    }

    /// <summary>
    /// Configures the core middleware pipeline.
    /// </summary>
    public TokenizationMiddlewareBuilder UseDefaultSecurity()
    {
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
        
        app.UseHttpsRedirection();
        app.UseCors(corsOptions.PolicyName);
        app.UseAntiforgery(); // CSRF protection
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSecurityHeaders();
        app.UseTokenizationIdempotency();
        
        return this;
    }
    
    /// <summary>
    /// Configures the core middleware pipeline.
    /// </summary>
    public TokenizationMiddlewareBuilder UseLogging()
    {
        // Enhanced Serilog-based logging (includes security monitoring)
        app.UseTokenizationLogging();
        return this;
    }
    
    /// <summary>
    /// Configures development-specific middleware (Swagger).
    /// </summary>
    public TokenizationMiddlewareBuilder UseSwagger()
    {
        if (app.IsSwaggerEnabled())
        {
            app.UseTokenizationSwagger(configuration);
        }
        return this;
    }

    /// <summary>
    /// Maps all application endpoints.
    /// </summary>
    public TokenizationMiddlewareBuilder MapEndpoints()
    {
        app.MapTokenizationEndpoints();
        return this;
    }
}

/// <summary>
/// Extension methods for the TokenizationMiddlewareBuilder.
/// </summary>
internal static class TokenizationMiddlewareBuilderExtensions
{
    /// <summary>
    /// Creates a new TokenizationMiddlewareBuilder instance.
    /// </summary>
    public static TokenizationMiddlewareBuilder UseTokenizationApi(this WebApplication app,
        IConfiguration configuration)
    {
        return new TokenizationMiddlewareBuilder(app, configuration);
    }
}