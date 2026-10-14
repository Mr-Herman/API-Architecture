namespace EAP.Models.Entities.Materials;

public sealed class MaterialInformationEntity
{
    public string MaterialLot { get; set; } = string.Empty;
    public string? MaterialPn { get; set; }
    public decimal? Quantity { get; set; }
    public DateTime? ExpireTime { get; set; }
}
