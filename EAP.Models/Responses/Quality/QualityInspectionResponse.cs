namespace EAP.Models.Responses.Quality;

public sealed class QualityInspectionResponse
{
    public bool Passed { get; set; }

    public string InspectionType { get; set; } = string.Empty;

    public string EquipmentCode { get; set; } = string.Empty;

    public string EquipmentName { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }
}
