namespace EAP.Models.Entities.Tracking;

public sealed class ProductionMaterialEntity
{
    public string EquipmentId { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string ComponentPartNumber { get; set; } = string.Empty;
    public decimal ConsumptionFactor { get; set; }
    public string? MaterialLot { get; set; }
    public string MaterialDateCode { get; set; } = string.Empty;
    public string? MaterialPartNumber { get; set; }
    public decimal OriginalQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public DateTime? ExpireTime { get; set; }
    public int RequiredComponentCount { get; set; }
    public int LoadedComponentCount { get; set; }
}
