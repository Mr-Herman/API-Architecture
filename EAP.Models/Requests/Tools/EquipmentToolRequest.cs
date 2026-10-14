using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Tools;

public sealed class EquipmentToolRequest
{
    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;
}
