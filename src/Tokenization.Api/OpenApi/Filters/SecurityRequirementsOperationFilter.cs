using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Tokenization.Api.OpenApi.Filters;

internal sealed class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var authAttributes = context.MethodInfo.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Concat(context.MethodInfo.DeclaringType?.GetCustomAttributes<AuthorizeAttribute>(inherit: true) ??
                    []);

        if (!authAttributes.Any()) return;

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                [new OpenApiSecuritySchemeReference("Bearer", null!, null!)] = []
            }
        };
    }
}
