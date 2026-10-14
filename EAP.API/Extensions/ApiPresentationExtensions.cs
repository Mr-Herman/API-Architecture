using System.Globalization;
using EAP.Api.Filters;
using EAP.Api.Localization;
using EAP.Application.Localization;
using EAP.Models.Constants;
using EAP.Models.Common;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace EAP.Api.Extensions;

public static class ApiPresentationExtensions
{
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddScoped<ActionTimingFilter>();
        services.AddControllers(options =>
        {
            options.Filters.AddService<ActionTimingFilter>();
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
        });
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = null;
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var localizer = context.HttpContext.RequestServices.GetRequiredService<IMessageLocalizer>();

                var result = ApiResult<object>.FailResult(
                    localizer["InvalidParameters"],
                    BusinessCode.ParamError);

                return new BadRequestObjectResult(result);
            };
        });

        return services;
    }

    public static IServiceCollection AddApiLocalization(this IServiceCollection services)
    {
        services.AddLocalization();
        services.AddSingleton<IMessageLocalizer, ResourceMessageLocalizer>();

        var supportedCultures = new[]
        {
            new CultureInfo("zh-CN"),
            new CultureInfo("en-US")
        };

        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture("zh-CN");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(ResolveRequestCultureAsync));
        });

        return services;
    }

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services, bool authenticationRequired)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => (type.FullName ?? type.Name).Replace('+', '.'));

            var bearerScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header
            };

            options.AddSecurityDefinition("Bearer", bearerScheme);
            options.OperationFilter<CommonResponseOperationFilter>();
            options.OperationFilter<AuthorizeOperationFilter>(authenticationRequired);
        });

        return services;
    }

    private static Task<ProviderCultureResult?> ResolveRequestCultureAsync(HttpContext context)
    {
        var language = context.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(language))
        {
            return Task.FromResult<ProviderCultureResult?>(null);
        }

        var culture = language.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en-US" : "zh-CN";

        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture));
    }
}
