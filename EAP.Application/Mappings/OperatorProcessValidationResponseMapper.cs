using EAP.Models.Responses.Operators;

namespace EAP.Application.Mappings;

internal static class OperatorProcessValidationResponseMapper
{
    public static OperatorProcessValidationResponse ToResponse(bool authenticated, string? failureReason)
    {
        return new OperatorProcessValidationResponse
        {
            Authenticated = authenticated,
            FailureReason = failureReason
        };
    }
}
