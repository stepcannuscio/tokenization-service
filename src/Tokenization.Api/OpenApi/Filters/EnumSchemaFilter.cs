using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using System.Reflection;

namespace Tokenization.Api.OpenApi.Filters;

/// <summary>
/// Schema filter to enhance enum documentation.
/// </summary>
internal sealed class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum) return;
        
        var enumValues = Enum.GetValues(context.Type);
        var descriptions = new List<string>();
            
        foreach (var enumValue in enumValues)
        {
            var field = context.Type.GetField(enumValue.ToString()!);
            var description = field?.GetCustomAttribute<DescriptionAttribute>()?.Description;
            descriptions.Add($"{enumValue}: {description ?? enumValue.ToString()}");
        }
            
        schema.Description = string.Join("; ", descriptions);
    }
}