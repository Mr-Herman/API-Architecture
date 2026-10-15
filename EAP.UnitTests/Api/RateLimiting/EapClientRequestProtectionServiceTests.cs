using EAP.Api.Options;
using EAP.Api.RateLimiting;
using Microsoft.Extensions.Options;

namespace EAP.UnitTests.Api.RateLimiting;

public sealed class EapClientRequestProtectionServiceTests
{
    [Fact]
    public void CheckBusinessRequest_WhenSameRouteIsCalledTooSoon_RejectsSecondRequest()
    {
        var service = CreateService(out _);

        var first = service.CheckBusinessRequest("EQ-001", "POST fis/Production/TrackOut");
        var second = service.CheckBusinessRequest("EQ-001", "POST fis/Production/TrackOut");
        var otherRoute = service.CheckBusinessRequest("EQ-001", "POST fis/Production/TrackIn");

        Assert.True(first.Allowed);
        Assert.False(second.Allowed);
        Assert.Equal(RequestLimitReason.MinimumInterval, second.Reason);
        Assert.True(otherRoute.Allowed);
    }

    [Fact]
    public void CheckBusinessRequest_WhenThresholdIsReachedAcrossOldFixedWindowBoundary_RejectsRequest()
    {
        var service = CreateService(out var timeProvider, options =>
        {
            options.EapClient.RequestThreshold = 5;
            options.EapClient.ObservationWindowSeconds = 30;
        });
        const string endpoint = "POST fis/Production/TrackOut";

        service.CheckBusinessRequest("EAP-001", endpoint);
        timeProvider.Advance(TimeSpan.FromSeconds(29));

        service.CheckBusinessRequest("EAP-001", endpoint);
        service.CheckBusinessRequest("EAP-001", endpoint);
        var fourthRequest = service.CheckBusinessRequest("EAP-001", endpoint);
        Assert.NotEqual(RequestLimitReason.AbnormalWindow, fourthRequest.Reason);

        timeProvider.Advance(TimeSpan.FromSeconds(2));
        service.CheckBusinessRequest("EAP-001", endpoint);
        var thresholdRequest = service.CheckBusinessRequest("EAP-001", endpoint);

        Assert.False(thresholdRequest.Allowed);
        Assert.Equal(RequestLimitReason.AbnormalWindow, thresholdRequest.Reason);
    }

    [Fact]
    public void CheckBusinessRequest_WhenThreeWindowsAreAbnormal_BlocksEquipment()
    {
        var service = CreateService(out var timeProvider, options =>
        {
            options.EapClient.RequestThreshold = 2;
            options.EapClient.ObservationWindowSeconds = 10;
            options.EapClient.CooldownSeconds = 2;
            options.EapClient.ViolationPeriodSeconds = 60;
            options.EapClient.ViolationWindowsBeforeBan = 3;
            options.EapClient.BanSeconds = 300;
        });

        RequestLimitDecision? decision = null;
        for (var window = 0; window < 3; window++)
        {
            service.CheckBusinessRequest("EQ-001", "GET fis/Equipment/Status");
            decision = service.CheckBusinessRequest("EQ-001", "GET fis/Equipment/Status");
            if (window < 2)
            {
                timeProvider.Advance(TimeSpan.FromSeconds(10));
            }
        }

        Assert.NotNull(decision);
        Assert.False(decision.Allowed);
        Assert.Equal(RequestLimitReason.EapClientBlocked, decision.Reason);

        var otherRoute = service.CheckBusinessRequest("EQ-001", "POST fis/Production/TrackIn");
        Assert.False(otherRoute.Allowed);
        Assert.Equal(RequestLimitReason.EapClientBlocked, otherRoute.Reason);

        timeProvider.Advance(TimeSpan.FromSeconds(300));
        var recovered = service.CheckBusinessRequest("EQ-001", "POST fis/Production/TrackIn");
        Assert.True(recovered.Allowed);
    }

    [Fact]
    public void CheckTokenRequest_WhenLimitIsExceeded_BlocksOnlyThatEquipment()
    {
        var service = CreateService(out _, options => options.Token.PermitLimit = 3);

        Assert.True(service.CheckTokenRequest("EQ-001").Allowed);
        Assert.True(service.CheckTokenRequest("EQ-001").Allowed);
        Assert.True(service.CheckTokenRequest("EQ-001").Allowed);

        var blocked = service.CheckTokenRequest("EQ-001");
        var otherEquipment = service.CheckTokenRequest("EQ-002");

        Assert.False(blocked.Allowed);
        Assert.Equal(RequestLimitReason.TokenBlocked, blocked.Reason);
        Assert.True(otherEquipment.Allowed);
    }

    [Fact]
    public void CheckTokenRequest_WhenLimitIsReachedAcrossFixedWindowBoundary_BlocksClient()
    {
        var service = CreateService(out var timeProvider, options =>
        {
            options.Token.PermitLimit = 3;
            options.Token.WindowSeconds = 60;
        });

        Assert.True(service.CheckTokenRequest("EAP-001").Allowed);
        timeProvider.Advance(TimeSpan.FromSeconds(59));
        Assert.True(service.CheckTokenRequest("EAP-001").Allowed);
        Assert.True(service.CheckTokenRequest("EAP-001").Allowed);

        timeProvider.Advance(TimeSpan.FromSeconds(2));
        Assert.True(service.CheckTokenRequest("EAP-001").Allowed);
        var blocked = service.CheckTokenRequest("EAP-001");

        Assert.False(blocked.Allowed);
        Assert.Equal(RequestLimitReason.TokenBlocked, blocked.Reason);
    }

    [Fact]
    public async Task CheckBusinessRequest_WhenRequestsAreConcurrent_AllowsOnlyOne()
    {
        var service = CreateService(out _);

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() =>
                service.CheckBusinessRequest("EQ-001", "POST fis/Production/TrackOut")));

        var decisions = await Task.WhenAll(tasks);

        Assert.Single(decisions, decision => decision.Allowed);
        Assert.Equal(19, decisions.Count(decision => !decision.Allowed));
    }

    private static EapClientRequestProtectionService CreateService(
        out TestTimeProvider timeProvider,
        Action<RateLimitOptions>? configure = null)
    {
        var options = new RateLimitOptions
        {
            PermitLimit = 10000,
            WindowSeconds = 60,
            QueueLimit = 0
        };

        configure?.Invoke(options);
        timeProvider = new TestTimeProvider(new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero));

        return new EapClientRequestProtectionService(Options.Create(options), timeProvider);
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public TestTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public void Advance(TimeSpan timeSpan)
        {
            _utcNow = _utcNow.Add(timeSpan);
        }
    }
}
