namespace EAP.Models.Entities.Tracking;

public sealed class ProductionTrackingContextEntity
{
    public string EquipmentId { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;
    public string WorkFlow { get; set; } = string.Empty;
    public string Process { get; set; } = string.Empty;
    public string? ProcessTable { get; set; }
    public int TraceWay { get; set; }
    public int OnlineRework { get; set; }
    public string LotId { get; set; } = string.Empty;
    public string? ProjectCode { get; set; }
    public string? WorkOrder { get; set; }
    public string? ProductPartNumber { get; set; }
    public string? FormNo { get; set; }
    public int ProcessQuantity { get; set; }
    public string? CarrierId { get; set; }
}
