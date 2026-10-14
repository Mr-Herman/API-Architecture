namespace EAP.Models.Entities.Production;

public sealed class ProductionEquipmentOperationEntity
{
    public string? WorkFlow { get; set; }
    public string? Process { get; set; }
    public string? ProcessCode { get; set; }
    public int? TraceWay { get; set; }
    public int? ProcessLeadTime { get; set; }
    public int? OnlineRework { get; set; }
}
