using EAP.Models.Common;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EAP.Api.Filters;

internal static class SwaggerResponseFactory
{
    public static OpenApiResponse Create(string description, OperationFilterContext context)
    {
        var schema = context.SchemaGenerator.GenerateSchema(typeof(ApiResult<object>), context.SchemaRepository);
        return new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new() { Schema = schema }
            }
        };
    }
}
