using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Tokenization.Api.Versioning;

namespace Tokenization.Api.OpenApi.Filters;

internal sealed class ApiVersionHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];

        if (operation.Parameters.Any(parameter =>
                string.Equals(parameter.Name, VersioningParams.Header, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = VersioningParams.Header,
            In = ParameterLocation.Header,
            Required = false,
            Description = "Optional API version header. The v1 routes also work without this header.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String
            }
        });
    }
}
