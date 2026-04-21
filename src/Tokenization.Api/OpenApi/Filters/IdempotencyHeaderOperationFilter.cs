using System.Reflection;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Tokenization.Api.Idempotency;

namespace Tokenization.Api.OpenApi.Filters;

/// <summary>
/// Operation filter to add idempotency header documentation.
/// </summary>
internal sealed class IdempotencyHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var httpMethod = context.MethodInfo.GetCustomAttribute<HttpMethodAttribute>(inherit: true);
        if (httpMethod is null) return;
        if (!httpMethod.HttpMethods.Any(IdempotencyMiddleware.IsDataModifying)) return;

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = IdempotencyHeaders.IdempotencyKey,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Unique key to ensure idempotent operations. Use a UUID or other unique identifier.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" }
        });
    }
}
