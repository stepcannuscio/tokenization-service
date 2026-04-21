using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using Tokenization.Api.Config.Options;
using Tokenization.Api.OpenApi.Filters;

namespace Tokenization.Api.OpenApi;

internal static class ApiDocumentationBuilder
{
    public static void ConfigureSwagger(
        SwaggerGenOptions options,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var developmentAuthOptions = configuration.GetSection(DevelopmentAuthOptions.SectionName).Get<DevelopmentAuthOptions>() ??
                                     new DevelopmentAuthOptions();
        var authDescription = environment.IsDevelopment() && developmentAuthOptions.Enabled
            ? $"Development mode: send `Authorization: Bearer {developmentAuthOptions.BearerToken}`."
            : "Production mode: send a JWT bearer token issued by the configured OIDC provider.";

        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Tokenization Service API",
            Version = "v1.0",
            Description = """
                          A standalone payment tokenization service with tenant isolation,
                          envelope encryption, health checks, and operational safeguards.
                          Use Idempotency-Key on write endpoints. Versioning is available in the path and via X-API-Version.
                          """,
            Contact = new OpenApiContact
            {
                Name = "Project Maintainer"
            }
        });

        // Add security definitions
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = $"{authDescription} Example: \"Authorization: Bearer <token>\""
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }

        options.SchemaFilter<EnumSchemaFilter>();
        options.SchemaFilter<ExampleSchemaFilter>();

        options.OperationFilter<SecurityRequirementsOperationFilter>();
        options.OperationFilter<IdempotencyHeaderOperationFilter>();
        options.OperationFilter<ApiVersionHeaderOperationFilter>();
    }
}
