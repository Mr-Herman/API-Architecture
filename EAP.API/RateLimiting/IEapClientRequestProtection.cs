namespace EAP.Api.RateLimiting;

public interface IEapClientRequestProtection
{
    RequestLimitDecision CheckBusinessRequest(string eapClientId, string endpointKey);

    RequestLimitDecision CheckTokenRequest(string eapClientId);
}
