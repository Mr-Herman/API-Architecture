using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Responses.Materials;

public sealed class EquipmentMaterialResponse
{
    public string EquipmentId { get; set; } = string.Empty;
    public string? ComponentName { get; set; }
    public string? MaterialLot { get; set; }
    public string? MaterialPn { get; set; }
    public decimal? OriginalQuantity { get; set; }
    public decimal? RemainQuantity { get; set; }
    public DateTime? ExpireTime { get; set; }
}
