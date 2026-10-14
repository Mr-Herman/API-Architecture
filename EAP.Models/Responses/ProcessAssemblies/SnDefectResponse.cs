namespace EAP.Models.Responses.ProcessAssemblies;

public sealed class SnDefectResponse
{
    public string ProjCode { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string LotNo { get; set; } = string.Empty;
    public string Sn { get; set; } = string.Empty;
    public string DefectCode { get; set; } = string.Empty;
    public string Defect { get; set; } = string.Empty;
    public int DefectQty { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime TestTime { get; set; }
}
