namespace EAP.Api.RateLimiting;

public enum RequestLimitReason
{
    None,
    MinimumInterval,
    AbnormalWindow,
    EapClientBlocked,
    TokenBlocked
}

public sealed class RequestLimitDecision
{
    private RequestLimitDecision(bool allowed, RequestLimitReason reason, int retryAfterSeconds, bool shouldLog)
    {
        Allowed = allowed;
        Reason = reason;
        RetryAfterSeconds = retryAfterSeconds;
        ShouldLog = shouldLog;
    }

    public bool Allowed { get; }

    public RequestLimitReason Reason { get; }

    public int RetryAfterSeconds { get; }

    public bool ShouldLog { get; }

    public static RequestLimitDecision Allow()
    {
        return new RequestLimitDecision(true, RequestLimitReason.None, 0, false);
    }

    public static RequestLimitDecision Reject(RequestLimitReason reason, int retryAfterSeconds, bool shouldLog = false)
    {
        return new RequestLimitDecision(false, reason, retryAfterSeconds, shouldLog);
    }
}
