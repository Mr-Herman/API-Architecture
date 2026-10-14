namespace EAP.Models.Entities.Tracking;

public sealed class ProductionLotWipEntity
{
    public decimal LotProcessSequence { get; set; }
    public string LotId { get; set; } = string.Empty;
    public string WorkFlow { get; set; } = string.Empty;
    public int TraceWay { get; set; }
    public string Process { get; set; } = string.Empty;
    public string? ProjectCode { get; set; }
    public string? WorkOrder { get; set; }
    public string? ProductPartNumber { get; set; }
    public DateTime TrackInTime { get; set; }
    public int TrackInQuantity { get; set; }
    public int Status { get; set; }
    public string? OperatorId { get; set; }
    public string? Tester { get; set; }
    public string? RecipeName { get; set; }
    public string? CarrierId { get; set; }
    public int AllowOnlineRework { get; set; }
    public string? FormNo { get; set; }
    public string Remark { get; set; } = string.Empty;
    public string ProcessMarking { get; set; } = "N";
}
