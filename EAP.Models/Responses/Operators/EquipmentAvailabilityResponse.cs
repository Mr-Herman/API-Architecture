namespace EAP.Models.Responses.Operators;

public sealed class EquipmentAvailabilityResponse
{
    public bool Available { get; set; }

    public string? FailureReason { get; set; }
}
