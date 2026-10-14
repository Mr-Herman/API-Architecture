using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Equipment;

public sealed class EquipmentStatusChangeRequest
{
    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Status { get; set; } = string.Empty;

    [StringLength(50)]
    public string? SubStatus { get; set; }

    [StringLength(500)]
    public string? SubStatusDesc { get; set; }
}
