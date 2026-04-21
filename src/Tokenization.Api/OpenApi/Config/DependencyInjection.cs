using Tokenization.Api.Config.Options;

namespace Tokenization.Api.OpenApi.Config;

/// <summary>
/// DI registration for Open Api.
/// </summary>
internal static class DependencyInjection
{

    /// <summary>
    /// Configures API explorer and Swagger with security definitions and multi-version support.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="_">The configuration.</param>
    public static void AddTokenizationOpenApi(this IServiceCollection services, IConfiguration _)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(ApiDocumentationBuilder.ConfigureSwagger);
    }

    /// <summary>
    /// Configures Swagger with multi-version support.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="_">The configuration.</param>
    public static void UseTokenizationSwagger(this WebApplication app, IConfiguration _)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.RoutePrefix = "swagger";
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tokenization API v1.0");

            // Add version selector dropdown
            c.DisplayRequestDuration();
            c.EnableDeepLinking();
            c.EnableFilter();
            c.ShowExtensions();
            c.EnableValidator();
        });
    }

    /// <summary>
    /// Checks if swagger is enabled.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>A bool indicating if swagger is enabled.</returns>
    public static bool IsSwaggerEnabled(this WebApplication app)
    {
        var swaggerOptions = app.Configuration.GetSection(SwaggerOptions.SectionName).Get<SwaggerOptions>() ??
                             new SwaggerOptions();

        return app.Environment.IsDevelopment() || swaggerOptions.Enabled;
    }
}