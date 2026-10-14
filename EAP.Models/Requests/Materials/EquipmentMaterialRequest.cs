using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Materials;

public sealed class EquipmentMaterialRequest
{
    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;
}
