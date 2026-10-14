using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EAP.Api.Filters;

public sealed class AuthorizeOperationFilter : IOperationFilter
{
    private readonly bool _authenticationRequired;

    public AuthorizeOperationFilter(bool authenticationRequired)
    {
        _authenticationRequired = authenticationRequired;
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!_authenticationRequired)
        {
            return;
        }

        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any())
            return;

        var bearerScheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        var securityRequirement = new OpenApiSecurityRequirement();
        securityRequirement.Add(bearerScheme, Array.Empty<string>());

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            securityRequirement
        };

        operation.Responses.TryAdd("401", SwaggerResponseFactory.Create("Unauthorized", context));
        operation.Responses.TryAdd("403", SwaggerResponseFactory.Create("Forbidden", context));
    }
}
