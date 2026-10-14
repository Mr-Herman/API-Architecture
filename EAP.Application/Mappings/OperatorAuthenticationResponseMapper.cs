using EAP.Models.Entities.Operators;
using EAP.Models.Responses.Operators;

namespace EAP.Application.Mappings;

internal static class OperatorAuthenticationResponseMapper
{
    public static OperatorAuthenticationResponse ToResponse(
        OperatorAccountEntity? account,
        bool authenticated,
        string? failureReason)
    {
        return new OperatorAuthenticationResponse
        {
            Authenticated = authenticated,
            OperatorName = account?.OperatorName,
            Roles = account?.Roles,
            FailureReason = failureReason
        };
    }

    public static OperatorAuthenticationResponse ToResponse(
        OperatorAuthenticationResponse account,
        bool authenticated,
        string? failureReason)
    {
        return new OperatorAuthenticationResponse
        {
            Authenticated = authenticated,
            OperatorName = account.OperatorName,
            Roles = account.Roles,
            FailureReason = failureReason
        };
    }
}
