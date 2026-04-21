using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using Tokenization.Api.OpenApi.Filters;

namespace Tokenization.Api.OpenApi;

/// <summary>
/// Configures OpenAPI documentation with comprehensive API standards.
/// </summary>
internal static class ApiDocumentationBuilder
{
    /// <summary>
    /// Configures Swagger with comprehensive API documentation standards.
    /// </summary>
    public static void ConfigureSwagger(SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Tokenization Service API",
            Version = "v1.0",
            Description = """
                          A standalone payment tokenization service with tenant isolation,
                          envelope encryption, health checks, and operational safeguards.
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
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
        });

        // Add global security requirement
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

        // Add XML comments
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }

        // Add custom schema filters
        options.SchemaFilter<EnumSchemaFilter>();

        // Add operation filters
        options.OperationFilter<SecurityRequirementsOperationFilter>();
        options.OperationFilter<IdempotencyHeaderOperationFilter>();
    }
}
