namespace EAP.Models.Entities.Materials;

public sealed class EquipmentMaterialEntity
{
    public string EquipmentId { get; set; } = string.Empty;
    public string? ComponentName { get; set; }
    public string? MaterialLot { get; set; }
    public string? MaterialPn { get; set; }
    public decimal? OriginalQuantity { get; set; }
    public decimal? RemainQuantity { get; set; }
    public DateTime? ExpireTime { get; set; }
}
