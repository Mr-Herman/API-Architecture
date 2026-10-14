namespace EAP.Models.Entities.Tools;

public sealed class EquipmentToolEntity
{
    public string? ToolId { get; set; }
    public decimal? LifeTimes { get; set; }
    public DateTime? ExpireTime { get; set; }
    public string? ToolPn { get; set; }
    public string? ToolType { get; set; }
    public string? ToolTypeName { get; set; }
}
