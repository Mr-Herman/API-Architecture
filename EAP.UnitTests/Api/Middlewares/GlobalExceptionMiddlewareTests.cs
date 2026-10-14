using System.Text.Json;
using EAP.Api.Logging;
using EAP.Api.Middlewares;
using EAP.Application.Exceptions;
using EAP.Application.Localization;
using EAP.Models.Constants;
using EAP.Models.Common;
using Microsoft.AspNetCore.Http;
using Moq;

namespace EAP.UnitTests.Tests;

public sealed class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenResourceIsMissing_Returns404ApiResult()
    {
        var middleware = CreateMiddleware(new ResourceNotFoundException("missing"));
        var context = CreateContext();

        await middleware.InvokeAsync(context, CreateLocalizer());

        var result = await ReadResponseAsync(context);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.False(result.Success);
        Assert.Equal(BusinessCode.NotFound, result.Code);
        Assert.Equal("missing", result.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenAuthenticationFails_Returns401ApiResult()
    {
        var middleware = CreateMiddleware(new AuthenticationFailedException("invalid credentials"));
        var context = CreateContext();

        await middleware.InvokeAsync(context, CreateLocalizer());

        var result = await ReadResponseAsync(context);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(result.Success);
        Assert.Equal(BusinessCode.JwtInvalid, result.Code);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledExceptionOccurs_Returns500ApiResult()
    {
        var middleware = CreateMiddleware(new InvalidOperationException("sensitive detail"));
        var context = CreateContext();

        await middleware.InvokeAsync(context, CreateLocalizer());

        var result = await ReadResponseAsync(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.False(result.Success);
        Assert.Equal(BusinessCode.ServerError, result.Code);
        Assert.Equal("InternalServerError", result.Message);
        Assert.DoesNotContain("sensitive", result.Message);

        var failure = RequestFailureContext.Get(context);
        Assert.NotNull(failure);
        Assert.Equal("UNHANDLED_ERROR", failure.LogType);
        Assert.Equal(BusinessCode.ServerError, failure.ErrorCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenClientCancels_MarksRequestAsCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var middleware = new GlobalExceptionMiddleware(_ => Task.FromCanceled(cancellation.Token));
        var context = CreateContext();
        context.RequestAborted = cancellation.Token;

        await middleware.InvokeAsync(context, CreateLocalizer());

        var failure = RequestFailureContext.Get(context);
        Assert.NotNull(failure);
        Assert.Equal("REQUEST_CANCELLED", failure.LogType);
        Assert.Equal("Client", failure.FailureLocation);
        Assert.Null(failure.ErrorCode);
    }

    private static GlobalExceptionMiddleware CreateMiddleware(Exception exception)
    {
        Task NextMiddleware(HttpContext context)
        {
            return Task.FromException(exception);
        }
        return new GlobalExceptionMiddleware(NextMiddleware);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static IMessageLocalizer CreateLocalizer()
    {
        var localizer = new Mock<IMessageLocalizer>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => key);
        return localizer.Object;
    }

    private static async Task<ApiResult<object>> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        var result = await JsonSerializer.DeserializeAsync<ApiResult<object>>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return Assert.IsType<ApiResult<object>>(result);
    }
}
