using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Responses.Production;

public sealed class LotInformationResponse
{
    public string? WorkFlow { get; set; }
    public int? TraceWay { get; set; }
    public string? Process { get; set; }
    public string? ProcessCode { get; set; }
    public int? ProcessLeadTime { get; set; }
    public string? BarcodeType { get; set; }
    public string? LotId { get; set; }
    public int? ProcessQuantity { get; set; }
    public string? ProjectCode { get; set; }
    public string? Recipe { get; set; }
    public string? WorkOrder { get; set; }
    public string? ProductPartNumber { get; set; }
    public string? FormNo { get; set; }
    public string? LotMarking { get; set; }
    public int? LotStatus { get; set; }
    public int? SnStatus { get; set; }
    public int? OnlineRework { get; set; }
    public IReadOnlyList<string> SerialNumbers { get; set; } = [];
    public IReadOnlyList<string> ToolRequirements { get; set; } = [];
    public IReadOnlyList<Item> MaterialRequirements { get; set; } = [];

    public sealed class Item
    {
        public string? ComponentName { get; set; }
        public string? ComponentPartNumber { get; set; }
        public decimal? ConsumptionFactor { get; set; }
    }
}
