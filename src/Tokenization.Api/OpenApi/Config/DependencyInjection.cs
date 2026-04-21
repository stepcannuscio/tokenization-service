using Tokenization.Api.Config.Options;

namespace Tokenization.Api.OpenApi.Config;

internal static class DependencyInjection
{
    public static void AddTokenizationOpenApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options => ApiDocumentationBuilder.ConfigureSwagger(options, configuration, environment));
    }

    public static void UseTokenizationSwagger(this WebApplication app, IConfiguration _)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.RoutePrefix = "swagger";
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tokenization API v1.0");
            c.DisplayRequestDuration();
            c.EnableDeepLinking();
            c.EnableFilter();
            c.ShowExtensions();
            c.EnableValidator();
        });
    }

    public static bool IsSwaggerEnabled(this WebApplication app)
    {
        var swaggerOptions = app.Configuration.GetSection(SwaggerOptions.SectionName).Get<SwaggerOptions>() ??
                             new SwaggerOptions();

        return app.Environment.IsDevelopment() || swaggerOptions.Enabled;
    }
}
