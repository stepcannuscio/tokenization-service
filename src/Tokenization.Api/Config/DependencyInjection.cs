using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Tokenization.Api.Config.Options;
using Tokenization.Infrastructure.Authorization;

namespace Tokenization.Api.Config;

/// <summary>
/// DI registration for the api layer.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Configures authentication and authorization services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    public static void AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        var securityOptions = configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ??
                              new SecurityOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authOptions.Authority;
                options.Audience = authOptions.Audience;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(securityOptions.JwtClockSkewMinutes)
                };
                
                // Configure events to normalize tenant claims for downstream access checks.
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var tenantId = ExtractTenantIdFromClaims(context.Principal);
                        if (string.IsNullOrEmpty(tenantId))
                        {
                            return Task.CompletedTask;
                        }

                        var identity = context.Principal?.Identity as ClaimsIdentity;
                        if (identity?.HasClaim(c => c.Type == TenantClaims.TenantId) != true)
                        {
                            identity?.AddClaim(new Claim(TenantClaims.TenantId, tenantId));
                        }

                        return Task.CompletedTask;
                    }
                };
            });
    }

    /// <summary>
    /// Configures CORS with strict security policies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    public static void AddTokenizationCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

        services.AddCors(options =>
        {
            options.AddPolicy(name: corsOptions.PolicyName, policy =>
            {
                policy.WithOrigins(corsOptions.AllowedOrigins)
                    .WithMethods(corsOptions.AllowedMethods)
                    .AllowAnyHeader()
                    .DisallowCredentials(); // JWTs are in Authorization header; no cookies
            });
        });
    }

    /// <summary>
    /// Configures essential security services using built-in ASP.NET Core features.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    public static void AddTokenizationSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var securityOptions = configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ??
                              new SecurityOptions();
        
        // Configure HTTPS redirection and HSTS
        services.AddHttpsRedirection(o => o.HttpsPort = securityOptions.HttpsPort);
        services.AddHsts(o =>
        {
            o.MaxAge = TimeSpan.FromDays(securityOptions.HstsMaxAgeDays);
        });
        
        // Add built-in CSRF protection
        services.AddAntiforgery();
    }

    /// <summary>
    /// Configures request size hardening for security protection against DoS attacks.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    public static void AddTokenizationRequestSizeHardening(this IServiceCollection services, IConfiguration configuration)
    {
        var requestSizeOptions = configuration.GetSection(RequestSizeOptions.SectionName).Get<RequestSizeOptions>() ??
                                 new RequestSizeOptions();

        // Configure form options for multipart and form data limits
        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = requestSizeOptions.MultipartBodyLengthLimit;
            options.ValueLengthLimit = requestSizeOptions.MaxFormValueLength;
            options.KeyLengthLimit = requestSizeOptions.MaxFormKeyLength;
            options.ValueCountLimit = requestSizeOptions.MaxFormFieldCount;
        });

        // Configure Kestrel server options for overall request body size
        services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = requestSizeOptions.MaxRequestBodySize;
        });
    }

    /// <summary>
    /// Extracts the canonical tenant ID from supported claim aliases.
    /// </summary>
    /// <param name="principal">The authenticated user's claims principal.</param>
    /// <returns>The tenant ID if found; otherwise, <c>null</c>.</returns>
    private static string? ExtractTenantIdFromClaims(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return principal.FindFirstValue(TenantClaims.TenantId) ??
               principal.FindFirstValue(TenantClaims.MerchantIdAlias) ??
               principal.FindFirstValue("tenantId") ??
               principal.FindFirstValue("org_id") ??
               principal.FindFirstValue("organization_id");
    }
}
