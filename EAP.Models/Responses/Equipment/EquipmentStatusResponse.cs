using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Responses.Equipment;

public sealed class EquipmentStatusResponse
{
    public string EquipmentId { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? SubStatus { get; set; }
    public string? SubStatusDesc { get; set; }
}
