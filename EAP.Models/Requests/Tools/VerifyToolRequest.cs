using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Tools;

public sealed class VerifyToolRequest
{
    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string ToolId { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int ToolUsage { get; set; }
}
