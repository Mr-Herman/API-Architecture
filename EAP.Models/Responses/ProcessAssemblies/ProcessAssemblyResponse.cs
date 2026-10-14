namespace EAP.Models.Responses.ProcessAssemblies;

public sealed class ProcessAssemblyResponse
{
    public long Id { get; set; }
    public string? JobNum { get; set; }
    public string? PartNo { get; set; }
    public string? ProjCode { get; set; }
    public string? ProductCode { get; set; }
    public string? ProcessId { get; set; }
    public string? ProductionLotNo { get; set; }
    public string? Marking { get; set; }
    public string? Tester { get; set; }
    public string? OperatorId { get; set; }
    public int? InputQty { get; set; }
    public int? OutputQty { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? TestTime { get; set; }
    public string? Remark { get; set; }
    public IReadOnlyList<SnDefectResponse> Defects { get; set; } = [];
}
