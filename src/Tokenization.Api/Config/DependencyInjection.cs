using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Tokenization.Api.Config.Options;
using Tokenization.Api.Authentication.Development;
using Tokenization.Infrastructure.Authorization;

namespace Tokenization.Api.Config;

internal static class DependencyInjection
{
    private const string AuthenticationScheme = "TokenizationAuthentication";

    public static void AddTokenizationAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();

        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        var developmentAuthOptions = configuration.GetSection(DevelopmentAuthOptions.SectionName).Get<DevelopmentAuthOptions>() ??
                                     new DevelopmentAuthOptions();
        var securityOptions = configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ??
                              new SecurityOptions();

        services.AddOptions<DevelopmentAuthOptions>()
            .Bind(configuration.GetSection(DevelopmentAuthOptions.SectionName))
            .Validate(options => !options.Enabled || environment.IsDevelopment(),
                "Development auth can only be enabled in the Development environment.")
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.BearerToken),
                "Development auth requires a non-empty bearer token.")
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.TenantId),
                "Development auth requires a tenant ID.")
            .ValidateOnStart();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AuthenticationScheme;
                options.DefaultChallengeScheme = AuthenticationScheme;
                options.DefaultScheme = AuthenticationScheme;
            })
            .AddPolicyScheme(AuthenticationScheme, AuthenticationScheme, options =>
            {
                options.ForwardDefaultSelector = _ =>
                    environment.IsDevelopment() && developmentAuthOptions.Enabled
                        ? DevelopmentAuthHandler.SchemeName
                        : JwtBearerDefaults.AuthenticationScheme;
            })
            .AddScheme<DevelopmentAuthSchemeOptions, DevelopmentAuthHandler>(
                DevelopmentAuthHandler.SchemeName,
                options =>
                {
                    options.Enabled = environment.IsDevelopment() && developmentAuthOptions.Enabled;
                    options.BearerToken = developmentAuthOptions.BearerToken;
                    options.UserId = developmentAuthOptions.UserId;
                    options.TenantId = developmentAuthOptions.TenantId;
                    options.Scopes = developmentAuthOptions.Scopes;
                    options.Roles = developmentAuthOptions.Roles;
                })
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

    public static void AddTokenizationSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var securityOptions = configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ??
                              new SecurityOptions();
        
        services.AddHttpsRedirection(o => o.HttpsPort = securityOptions.HttpsPort);
        services.AddHsts(o =>
        {
            o.MaxAge = TimeSpan.FromDays(securityOptions.HstsMaxAgeDays);
        });
        
        services.AddAntiforgery();
    }

    public static void AddTokenizationRequestSizeHardening(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var requestSizeOptions = configuration.GetSection(RequestSizeOptions.SectionName).Get<RequestSizeOptions>() ??
                                 new RequestSizeOptions();

        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = requestSizeOptions.MultipartBodyLengthLimit;
            options.ValueLengthLimit = requestSizeOptions.MaxFormValueLength;
            options.KeyLengthLimit = requestSizeOptions.MaxFormKeyLength;
            options.ValueCountLimit = requestSizeOptions.MaxFormFieldCount;
        });

        services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = requestSizeOptions.MaxRequestBodySize;
        });
    }

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
