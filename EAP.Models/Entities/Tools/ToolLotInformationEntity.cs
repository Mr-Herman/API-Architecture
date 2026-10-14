namespace EAP.Models.Entities.Tools;

public sealed class ToolLotInformationEntity
{
    public string? ToolId { get; set; }
    public string? EquipmentId { get; set; }
    public decimal? LifeTimes { get; set; }
    public decimal? LifeHours { get; set; }
    public string? ToolPn { get; set; }
    public string? ToolType { get; set; }
    public string? ToolTypeName { get; set; }
}
