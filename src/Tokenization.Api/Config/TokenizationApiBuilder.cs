using Microsoft.AspNetCore.Mvc;
using Tokenization.Api.Authorization.Config;
using Tokenization.Api.Controllers.Config;
using Tokenization.Api.Health.Config;
using Tokenization.Api.Idempotency.Config;
using Tokenization.Api.OpenApi.Config;
using Tokenization.Api.RateLimiting.Config;
using Tokenization.Api.Versioning.Config;
using Tokenization.Application.Config;
using Tokenization.Infrastructure.Config;

namespace Tokenization.Api.Config;

/// <summary>
/// Builder for configuring Tokenization API services and middleware.
/// Provides a fluent interface for configuring the API with proper separation of concerns.
/// </summary>
internal sealed class TokenizationApiBuilder(IServiceCollection services, IConfiguration configuration)
{
    /// <summary>
    /// Configures controllers.
    /// </summary>
    public TokenizationApiBuilder AddControllers()
    {
        services.AddTokenizationControllers();
        return this;
    }
    
    /// <summary>
    /// Configures API versioning.
    /// </summary>
    public TokenizationApiBuilder AddVersioning()
    {
        services.AddTokenizationVersioning();
        return this;
    }
        
    /// <summary>
    /// Configures <see cref="ProblemDetails"/> for failed requests.
    /// </summary>
    public TokenizationApiBuilder AddProblemDetails()
    {
        services.AddProblemDetails();
        return this;
    }

    /// <summary>
    /// Configures authentication and authorization.
    /// </summary>
    public TokenizationApiBuilder AddAuthentication()
    {
        services.AddAuthentication(configuration);
        services.AddTokenizationAuthorization();
        return this;
    }

    /// <summary>
    /// Configures security services (HTTPS, HSTS, CSRF).
    /// </summary>
    public TokenizationApiBuilder AddSecurity()
    {
        services.AddTokenizationSecurity(configuration);
        return this;
    }

    /// <summary>
    /// Configures CORS policies.
    /// </summary>
    public TokenizationApiBuilder AddCors()
    {
        services.AddTokenizationCors(configuration);
        return this;
    }

    /// <summary>
    /// Configures rate limiting.
    /// </summary>
    public TokenizationApiBuilder AddRateLimiting()
    {
        services.AddTokenizationRateLimiting();
        return this;
    }

    /// <summary>
    /// Configures idempotency support.
    /// </summary>
    public TokenizationApiBuilder AddIdempotency()
    {
        services.AddTokenizationIdempotency(configuration);
        return this;
    }

    /// <summary>
    /// Configures Swagger/OpenAPI documentation.
    /// </summary>
    public TokenizationApiBuilder AddOpenApi()
    {
        services.AddTokenizationOpenApi(configuration);
        return this;
    }

    /// <summary>
    /// Configures request size limits.
    /// </summary>
    public TokenizationApiBuilder AddRequestSizeLimits()
    {
        services.AddTokenizationRequestSizeHardening(configuration);
        return this;
    }

    /// <summary>
    /// Configures health checks.
    /// </summary>
    public TokenizationApiBuilder AddHealthChecks()
    {
        services.AddApiHealthCheck(configuration);
        return this;
    }
    
    /// <summary>
    /// Configures infra.
    /// </summary>
    public TokenizationApiBuilder AddInfra()
    {
        services.AddTokenizationInfra(configuration);
        return this;
    }
        
    /// <summary>
    /// Configures application services (for business logic).
    /// </summary>
    public TokenizationApiBuilder AddApplication()
    {
        services.AddTokenizationApplication();
        return this;
    }
}

/// <summary>
/// Extension methods for the TokenizationApiBuilder.
/// </summary>
internal static class TokenizationApiBuilderExtensions
{
    /// <summary>
    /// Creates a new TokenizationApiBuilder instance.
    /// </summary>
    public static TokenizationApiBuilder AddTokenizationApi(this IServiceCollection services, IConfiguration configuration)
    {
        return new TokenizationApiBuilder(services, configuration);
    }
}