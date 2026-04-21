using Tokenization.Infrastructure.Authorization;
using Serilog;
using Serilog.Events;

namespace Tokenization.Api.Logging.Config;

/// <summary>
/// DI registration for logging.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Configures logging services using Serilog with structured logging and PCI-safe sanitization.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    public static void AddTokenizationLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
            .CreateLogger();


        builder.Host.UseSerilog((ctx, services, cfg) =>
        {
            cfg.ReadFrom.Configuration(ctx.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Destructure.With(new SensitiveDestructuringPolicy());
        });
    }
    
    /// <summary>
    /// Configures Serilog request logging (no bodies; custom level + template).
    /// </summary>
    /// <param name="app">The web application.</param>
    public static void UseTokenizationLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(opts =>
        {
            // Lower noise for common 200s, bump for server errors
            opts.GetLevel = (httpCtx, _, ex) =>
            {
                if (ex is not null || httpCtx.Response.StatusCode >= 500) return LogEventLevel.Error;
                return httpCtx.Response.StatusCode >= 400 ? LogEventLevel.Warning : LogEventLevel.Information;
            };

            // Keep the message minimal—no headers or bodies here
            opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0} ms";
            
            opts.EnrichDiagnosticContext = (diag, context) =>
            {
                diag.Set(LogProperties.RequestId, context.TraceIdentifier);
                
                var userId = context.User.FindFirst("sub")?.Value ?? context.User.Identity?.Name;
                if (userId != null) diag.Set(LogProperties.UserId, userId);
                
                var tenantId = context.User.FindFirst(TenantClaims.TenantId)?.Value;
                if (tenantId != null) diag.Set(LogProperties.TenantId, tenantId);

                var clientIp = context.Connection.RemoteIpAddress?.ToString();
                if (clientIp != null) diag.Set(LogProperties.ClientIp, clientIp);
                
                // Add security context for monitoring (only if non-default values)
                var userAgent = context.Request.Headers.UserAgent.ToString();
                if (!string.IsNullOrEmpty(userAgent) && userAgent != "unknown") 
                    diag.Set(LogProperties.UserAgent, userAgent);
                
                var requestSize = context.Request.ContentLength;
                if (requestSize is > 0) 
                    diag.Set(LogProperties.RequestSize, requestSize.Value);
                
                var responseSize = context.Response.ContentLength;
                if (responseSize is > 0) 
                    diag.Set(LogProperties.ResponseSize, responseSize.Value);
            };
        });
    }
}