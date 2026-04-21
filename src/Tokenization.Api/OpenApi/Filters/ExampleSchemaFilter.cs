using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace Tokenization.Api.OpenApi.Filters;

internal sealed class ExampleSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        _ = schema;
        _ = context;
    }
}
