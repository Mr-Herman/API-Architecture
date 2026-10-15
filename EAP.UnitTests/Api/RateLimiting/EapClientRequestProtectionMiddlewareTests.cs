using System.Security.Claims;
using EAP.Api.RateLimiting;
using EAP.Application.Localization;
using EAP.Models.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EAP.UnitTests.Api.RateLimiting;

public sealed class EapClientRequestProtectionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenAuthenticatedTokenHasNoClientId_Returns403()
    {
        var nextCalled = false;
        var middleware = new EapClientRequestProtectionMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<EapClientRequestProtectionMiddleware>.Instance);
        var context = CreateAuthenticatedContext(
            new Claim(ClaimTypes.NameIdentifier, "legacy-client"));
        var protection = new Mock<IEapClientRequestProtection>();
        var localizer = new Mock<IMessageLocalizer>();
        localizer.Setup(value => value["Forbidden"]).Returns("Forbidden");

        await middleware.InvokeAsync(context, protection.Object, localizer.Object);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.False(nextCalled);
        protection.Verify(
            service => service.CheckBusinessRequest(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenClientIdIsPresentAndRequestIsAllowed_CallsNextMiddleware()
    {
        var nextCalled = false;
        var middleware = new EapClientRequestProtectionMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<EapClientRequestProtectionMiddleware>.Instance);
        var context = CreateAuthenticatedContext(
            new Claim(JwtClaimNames.EapClientId, "EAP-001"));
        var protection = new Mock<IEapClientRequestProtection>();
        protection
            .Setup(service => service.CheckBusinessRequest("EAP-001", "POST fis/Test"))
            .Returns(RequestLimitDecision.Allow());
        var localizer = new Mock<IMessageLocalizer>();

        await middleware.InvokeAsync(context, protection.Object, localizer.Object);

        Assert.True(nextCalled);
        protection.Verify(
            service => service.CheckBusinessRequest("EAP-001", "POST fis/Test"),
            Times.Once);
    }

    private static DefaultHttpContext CreateAuthenticatedContext(params Claim[] claims)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        context.SetEndpoint(new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("fis/Test"),
            0,
            new EndpointMetadataCollection(),
            "Test endpoint"));

        return context;
    }
}
