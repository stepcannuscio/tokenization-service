using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Tokenization.Api.Requests.v1;
using Tokenization.Api.Responses;

namespace Tokenization.Api.OpenApi.Filters;

internal sealed class ExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(CreateTokenRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["pan"] = new OpenApiString("4111111111111111"),
                ["expirationMonth"] = new OpenApiInteger(12),
                ["expirationYear"] = new OpenApiInteger(2030),
                ["cardholderName"] = new OpenApiString("Alex Example"),
                ["network"] = new OpenApiString("Visa"),
                ["customerId"] = new OpenApiString("customer-123"),
                ["paymentMethodType"] = new OpenApiString("Card"),
                ["tokenType"] = new OpenApiString("OneTime"),
                ["currency"] = new OpenApiString("USD"),
                ["country"] = new OpenApiString("US"),
                ["maxUses"] = new OpenApiInteger(1)
            };
        }

        if (context.Type == typeof(CreateTokenResponse))
        {
            schema.Example = new OpenApiObject
            {
                ["token"] = new OpenApiString("tok_01JW3H6X6S0S5F6M4H0C5J0G8D"),
                ["maskedData"] = new OpenApiString("411111******1111"),
                ["last4"] = new OpenApiString("1111"),
                ["paymentMethodType"] = new OpenApiString("Card"),
                ["network"] = new OpenApiString("Visa")
            };
        }

        if (context.Type == typeof(GetTokenResponse))
        {
            schema.Example = new OpenApiObject
            {
                ["token"] = new OpenApiString("tok_01JW3H6X6S0S5F6M4H0C5J0G8D"),
                ["maskedData"] = new OpenApiString("411111******1111"),
                ["last4"] = new OpenApiString("1111"),
                ["paymentMethodType"] = new OpenApiString("Card"),
                ["network"] = new OpenApiString("Visa"),
                ["customerId"] = new OpenApiString("customer-123"),
                ["tenantId"] = new OpenApiString("demo-tenant"),
                ["createdAt"] = new OpenApiDateTime(DateTimeOffset.Parse("2026-04-21T18:40:00Z")),
                ["maxUses"] = new OpenApiInteger(1),
                ["usageCount"] = new OpenApiInteger(0)
            };
        }

        if (context.Type == typeof(DetokenizeTokenResponse))
        {
            schema.Example = new OpenApiObject
            {
                ["pan"] = new OpenApiString("4111111111111111"),
                ["expMonth"] = new OpenApiInteger(12),
                ["expYear"] = new OpenApiInteger(2030),
                ["cardholderName"] = new OpenApiString("Alex Example"),
                ["paymentMethodType"] = new OpenApiString("Card"),
                ["network"] = new OpenApiString("Visa")
            };
        }
    }
}
