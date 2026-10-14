using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Equipment;

public sealed class EquipmentQueryRequest
{
    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;
}
