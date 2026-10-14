using EAP.Models.Responses.Quality;

namespace EAP.Application.Mappings;

internal static class QualityInspectionResponseMapper
{
    public static QualityInspectionResponse ToResponse(
        bool passed,
        string inspectionType,
        string equipmentId,
        string? reason,
        DateTimeOffset startTime)
    {
        return new QualityInspectionResponse
        {
            Passed = passed,
            InspectionType = inspectionType,
            EquipmentCode = equipmentId,
            EquipmentName = equipmentId,
            Reason = reason,
            StartTime = startTime,
            EndTime = DateTimeOffset.Now
        };
    }
}
