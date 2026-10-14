namespace EAP.Models.Entities.Production;

public sealed class ProductionLotEntity
{
    public string? LotId { get; set; }
    public string? WorkOrder { get; set; }
    public string? ProductPartNumber { get; set; }
    public string? IsFinished { get; set; }
    public string? FormNo { get; set; }
    public string? ProjectCode { get; set; }
    public string? WorkFlow { get; set; }
    public int? ProcessQuantity { get; set; }
    public string? LotMarking { get; set; }
    public string? SerialNumber { get; set; }
}
