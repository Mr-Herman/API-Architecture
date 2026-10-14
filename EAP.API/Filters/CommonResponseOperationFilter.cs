using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EAP.Api.Filters;

public sealed class CommonResponseOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses.TryAdd("400", SwaggerResponseFactory.Create("Invalid request", context));
        operation.Responses.TryAdd("429", SwaggerResponseFactory.Create("Too many requests", context));
        operation.Responses.TryAdd("500", SwaggerResponseFactory.Create("Internal server error", context));

        if (context.ApiDescription.RelativePath?.Contains("{id}", StringComparison.OrdinalIgnoreCase) == true)
        {
            operation.Responses.TryAdd("404", SwaggerResponseFactory.Create("Resource not found", context));
        }
    }
}
