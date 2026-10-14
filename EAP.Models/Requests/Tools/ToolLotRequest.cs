using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Tools;

public sealed class ToolLotRequest
{
    [Required, StringLength(50)]
    public string ToolId { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;
}
