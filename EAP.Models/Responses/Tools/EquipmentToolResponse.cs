using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Responses.Tools;

public sealed class EquipmentToolResponse
{
    public string? ToolId { get; set; }
    public decimal? LifeTimes { get; set; }
    public DateTime? ExpireTime { get; set; }
    public string? ToolPn { get; set; }
    public string? ToolType { get; set; }
    public string? ToolTypeName { get; set; }
}
