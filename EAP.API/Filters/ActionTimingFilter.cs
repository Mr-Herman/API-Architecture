using EAP.Application.Logging;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EAP.Api.Filters;

public sealed class ActionTimingFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controller = context.Controller.GetType().Name;
        var action = context.ActionDescriptor.RouteValues["action"] ?? "UnknownAction";

        using var timing = MethodTimingScope.Start("Controller", action, controller);

        await next();
    }
}
