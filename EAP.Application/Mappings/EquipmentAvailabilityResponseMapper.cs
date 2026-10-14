using EAP.Models.Responses.Operators;

namespace EAP.Application.Mappings;

internal static class EquipmentAvailabilityResponseMapper
{
    public static EquipmentAvailabilityResponse ToResponse(bool available, string? failureReason)
    {
        return new EquipmentAvailabilityResponse
        {
            Available = available,
            FailureReason = failureReason
        };
    }
}
