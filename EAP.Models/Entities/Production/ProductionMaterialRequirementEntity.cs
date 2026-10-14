namespace EAP.Models.Entities.Production;

public sealed class ProductionMaterialRequirementEntity
{
    public string? ComponentName { get; set; }
    public string? ComponentPartNumber { get; set; }
    public decimal? ConsumptionFactor { get; set; }
}
