using System.Diagnostics;
using EAP.Api.Logging;
using EAP.Application.Logging;
using Microsoft.AspNetCore.Http;

namespace EAP.UnitTests.Tests
{
    public sealed class RequestLoggingContextTests
    {
        [Fact]
        public void MethodTimingScope_RecordsMethodsInStartOrder()
        {
            using var activity = new Activity("request-test").Start();

            using (MethodTimingScope.Start("Controller", "Get", "TestController"))
            {
                using (MethodTimingScope.Start("Application", "GetAsync", "TestService"))
                {
                }
            }

            var timings = MethodTimingScope.GetTimings(activity);

            Assert.Equal(2, timings.Count);
            Assert.Equal("Controller", timings[0].Layer);
            Assert.Equal("Get", timings[0].MethodName);
            Assert.Equal("Application", timings[1].Layer);
            Assert.Equal("GetAsync", timings[1].MethodName);
        }

        [Fact]
        public async Task RequestFailureContext_IdentifiesApplicationMethod()
        {
            var context = new DefaultHttpContext();

            Exception exception;
            try
            {
                await EAP.Application.TestDoubles.FailingService.ExecuteAsync();
                throw new InvalidOperationException("Expected test exception was not thrown.");
            }
            catch (TestFailureException caught)
            {
                exception = caught;
            }

            RequestFailureContext.Set(context, "UNHANDLED_ERROR", exception, 5002);
            var failure = RequestFailureContext.Get(context);

            Assert.NotNull(failure);
            Assert.Equal("UNHANDLED_ERROR", failure.LogType);
            Assert.Equal(5002, failure.ErrorCode);
            Assert.Equal("Application[FailingService].ExecuteAsync", failure.FailureLocation);
        }

        [Fact]
        public async Task RequestFailureContext_IdentifiesSecurityMethod()
        {
            var context = new DefaultHttpContext();

            Exception exception;
            try
            {
                await EAP.Infrastructure.Security.TestDoubles.FailingTokenService.ExecuteAsync();
                throw new InvalidOperationException("Expected test exception was not thrown.");
            }
            catch (TestFailureException caught)
            {
                exception = caught;
            }

            RequestFailureContext.Set(context, "UNHANDLED_ERROR", exception, 5002);
            var failure = RequestFailureContext.Get(context);

            Assert.NotNull(failure);
            Assert.Equal("Security[FailingTokenService].ExecuteAsync", failure.FailureLocation);
        }
    }

    public sealed class TestFailureException : Exception
    {
        public TestFailureException(string message) : base(message)
        {
        }
    }
}

namespace EAP.Application.TestDoubles
{
    public static class FailingService
    {
        public static async Task ExecuteAsync()
        {
            await Task.Yield();
            throw new EAP.UnitTests.Tests.TestFailureException("Application failed.");
        }
    }
}

namespace EAP.Infrastructure.Security.TestDoubles
{
    public static class FailingTokenService
    {
        public static async Task ExecuteAsync()
        {
            await Task.Yield();
            throw new EAP.UnitTests.Tests.TestFailureException("Security failed.");
        }
    }
}
