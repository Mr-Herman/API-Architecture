namespace EAP.Models.Entities.Equipment;

public sealed class EquipmentStatusEntity
{
    public string EquipmentId { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? SubStatus { get; set; }
    public string? SubStatusDesc { get; set; }
}
