using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BusBooking.Api.OpenApi;

internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(      // It is an OpenAPI (Swagger) document transformer that automatically adds JWT Bearer Authentication to your Swagger/OpenAPI documentation.
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var components = document.Components ??= new OpenApiComponents();
        components.SecuritySchemes!["BearerAuth"] = new OpenApiSecurityScheme
        {
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            Description  = "Enter a valid JWT Bearer token issued by the configured Entra ID tenant.",
        };

        // OpenApi v2: security requirement keys are OpenApiSecuritySchemeReference,
        // not OpenApiSecurityScheme with an embedded Reference property.
        var requirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("BearerAuth", document)] = [],
        };

        if (document.Paths is null) return Task.CompletedTask;

        foreach (var path in document.Paths.Values)
        {
            foreach (var operation in path.Operations!.Values)
            {
                operation.Security ??= [];
                operation.Security.Add(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
