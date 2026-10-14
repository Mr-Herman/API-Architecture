namespace EAP.Models.Entities.Tracking;

public sealed class ProductionSnWipEntity
{
    public decimal SnProcessSequence { get; set; }
    public decimal LotProcessSequence { get; set; }
    public string LotId { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string WorkFlow { get; set; } = string.Empty;
    public int TraceWay { get; set; }
    public string Process { get; set; } = string.Empty;
    public DateTime LoadingTime { get; set; }
    public DateTime? TrackInTime { get; set; }
    public int Status { get; set; }
    public string? Tester { get; set; }
    public string? ProjectCode { get; set; }
    public string? WorkOrder { get; set; }
    public string? ProductPartNumber { get; set; }
    public string? ProcessResult { get; set; }
    public string? DefectCode { get; set; }
    public string? DefectName { get; set; }
    public string? TestDefect { get; set; }
    public int OnlineRework { get; set; }
    public string? OperatorId { get; set; }
    public string? RecipeName { get; set; }
    public string? CarrierId { get; set; }
}
