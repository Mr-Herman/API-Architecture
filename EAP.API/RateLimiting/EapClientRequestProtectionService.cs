using System.Collections.Concurrent;
using EAP.Api.Options;
using Microsoft.Extensions.Options;

namespace EAP.Api.RateLimiting;

public sealed class EapClientRequestProtectionService : IEapClientRequestProtection
{
    private readonly ConcurrentDictionary<string, EapClientRequestState> _states = new(StringComparer.OrdinalIgnoreCase);
    private readonly RateLimitOptions _options;
    private readonly TimeProvider _timeProvider;
    private long _nextCleanupUtcTicks;

    public EapClientRequestProtectionService(IOptions<RateLimitOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public RequestLimitDecision CheckBusinessRequest(string eapClientId, string endpointKey)
    {
        if (!_options.EapClient.Enabled)
        {
            return RequestLimitDecision.Allow();
        }

        var now = _timeProvider.GetUtcNow();
        RemoveExpiredStates(now);

        var state = _states.GetOrAdd(eapClientId, _ => new EapClientRequestState(now));
        lock (state.SyncRoot)
        {
            state.LastSeenUtc = now;

            if (state.BlockedUntilUtc > now)
            {
                return RequestLimitDecision.Reject(
                    RequestLimitReason.EapClientBlocked,
                    SecondsUntil(state.BlockedUntilUtc, now));
            }

            var routeState = GetRouteState(state, endpointKey);
            RecordRequest(routeState, now);

            var abnormalDecision = EvaluateAbnormalTraffic(state, routeState, now);
            if (abnormalDecision is not null)
            {
                return abnormalDecision;
            }

            if (routeState.CooldownUntilUtc > now)
            {
                return RequestLimitDecision.Reject(
                    RequestLimitReason.AbnormalWindow,
                    SecondsUntil(routeState.CooldownUntilUtc, now));
            }

            var interval = ResolveRequestInterval(endpointKey);
            var nextAllowedAt = routeState.LastAcceptedUtc + interval;
            if (routeState.LastAcceptedUtc != DateTimeOffset.MinValue && nextAllowedAt > now)
            {
                return RequestLimitDecision.Reject(
                    RequestLimitReason.MinimumInterval,
                    SecondsUntil(nextAllowedAt, now));
            }

            routeState.LastAcceptedUtc = now;
            return RequestLimitDecision.Allow();
        }
    }

    public RequestLimitDecision CheckTokenRequest(string eapClientId)
    {
        var now = _timeProvider.GetUtcNow();
        RemoveExpiredStates(now);

        var state = _states.GetOrAdd(eapClientId, _ => new EapClientRequestState(now));
        lock (state.SyncRoot)
        {
            state.LastSeenUtc = now;

            if (state.TokenBlockedUntilUtc > now)
            {
                return RequestLimitDecision.Reject(
                    RequestLimitReason.TokenBlocked,
                    SecondsUntil(state.TokenBlockedUntilUtc, now));
            }

            var tokenWindow = TimeSpan.FromSeconds(_options.Token.WindowSeconds);
            RemoveExpiredRequests(state.TokenRequestTimes, now, tokenWindow);
            state.TokenRequestTimes.Enqueue(now);
            if (state.TokenRequestTimes.Count <= _options.Token.PermitLimit)
            {
                return RequestLimitDecision.Allow();
            }

            state.TokenBlockedUntilUtc = now.AddSeconds(_options.Token.BanSeconds);
            state.TokenRequestTimes.Clear();
            return RequestLimitDecision.Reject(
                RequestLimitReason.TokenBlocked,
                _options.Token.BanSeconds,
                true);
        }
    }

    private RequestLimitDecision? EvaluateAbnormalTraffic(EapClientRequestState state, RouteRequestState routeState, DateTimeOffset now)
    {
        if (routeState.RequestTimes.Count < _options.EapClient.RequestThreshold
            || routeState.NextViolationAllowedUtc > now)
        {
            return null;
        }

        routeState.NextViolationAllowedUtc = now.AddSeconds(_options.EapClient.ObservationWindowSeconds);
        routeState.CooldownUntilUtc = now.AddSeconds(_options.EapClient.CooldownSeconds);

        var violationPeriod = TimeSpan.FromSeconds(_options.EapClient.ViolationPeriodSeconds);
        while (state.ViolationTimes.Count > 0 && now - state.ViolationTimes.Peek() > violationPeriod)
        {
            state.ViolationTimes.Dequeue();
        }

        state.ViolationTimes.Enqueue(now);
        if (state.ViolationTimes.Count < _options.EapClient.ViolationWindowsBeforeBan)
        {
            return RequestLimitDecision.Reject(RequestLimitReason.AbnormalWindow, _options.EapClient.CooldownSeconds, true);
        }

        state.BlockedUntilUtc = now.AddSeconds(_options.EapClient.BanSeconds);
        state.ViolationTimes.Clear();

        return RequestLimitDecision.Reject(RequestLimitReason.EapClientBlocked, _options.EapClient.BanSeconds, true);
    }

    private void RecordRequest(RouteRequestState routeState, DateTimeOffset now)
    {
        var observationWindow = TimeSpan.FromSeconds(_options.EapClient.ObservationWindowSeconds);
        RemoveExpiredRequests(routeState.RequestTimes, now, observationWindow);

        routeState.RequestTimes.Enqueue(now);
        while (routeState.RequestTimes.Count > _options.EapClient.RequestThreshold)
        {
            routeState.RequestTimes.Dequeue();
        }
    }

    private static void RemoveExpiredRequests(Queue<DateTimeOffset> requestTimes, DateTimeOffset now, TimeSpan window)
    {
        while (requestTimes.Count > 0 && now - requestTimes.Peek() >= window)
        {
            requestTimes.Dequeue();
        }
    }

    private TimeSpan ResolveRequestInterval(string endpointKey)
    {
        if (_options.EapClient.RouteIntervalSeconds.TryGetValue(endpointKey, out var seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        return TimeSpan.FromSeconds(_options.EapClient.DefaultIntervalSeconds);
    }

    private static RouteRequestState GetRouteState(EapClientRequestState state, string endpointKey)
    {
        if (state.Routes.TryGetValue(endpointKey, out var routeState))
        {
            return routeState;
        }

        routeState = new RouteRequestState();
        state.Routes.Add(endpointKey, routeState);
        return routeState;
    }

    private void RemoveExpiredStates(DateTimeOffset now)
    {
        var nextCleanup = Interlocked.Read(ref _nextCleanupUtcTicks);
        if (now.UtcTicks < nextCleanup)
        {
            return;
        }

        var cleanupInterval = TimeSpan.FromMinutes(1);
        if (Interlocked.CompareExchange(ref _nextCleanupUtcTicks, now.Add(cleanupInterval).UtcTicks, nextCleanup) != nextCleanup)
        {
            return;
        }

        var expiration = TimeSpan.FromMinutes(_options.EapClient.StateExpirationMinutes);
        foreach (var item in _states)
        {
            lock (item.Value.SyncRoot)
            {
                if (now - item.Value.LastSeenUtc >= expiration
                    && item.Value.BlockedUntilUtc <= now
                    && item.Value.TokenBlockedUntilUtc <= now)
                {
                    _states.TryRemove(item.Key, out _);
                }
            }
        }
    }

    private static int SecondsUntil(DateTimeOffset target, DateTimeOffset now)
    {
        return Math.Max(1, (int)Math.Ceiling((target - now).TotalSeconds));
    }

    private sealed class EapClientRequestState
    {
        public EapClientRequestState(DateTimeOffset now)
        {
            LastSeenUtc = now;
        }

        public object SyncRoot { get; } = new();

        public Dictionary<string, RouteRequestState> Routes { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Queue<DateTimeOffset> ViolationTimes { get; } = new();

        public DateTimeOffset LastSeenUtc { get; set; }

        public DateTimeOffset BlockedUntilUtc { get; set; }

        public Queue<DateTimeOffset> TokenRequestTimes { get; } = new();

        public DateTimeOffset TokenBlockedUntilUtc { get; set; }
    }

    private sealed class RouteRequestState
    {
        public Queue<DateTimeOffset> RequestTimes { get; } = new();

        public DateTimeOffset NextViolationAllowedUtc { get; set; }

        public DateTimeOffset CooldownUntilUtc { get; set; }

        public DateTimeOffset LastAcceptedUtc { get; set; }
    }
}
