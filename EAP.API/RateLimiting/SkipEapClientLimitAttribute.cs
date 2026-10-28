namespace EAP.Api.RateLimiting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SkipEapClientLimitAttribute : Attribute
{
}
