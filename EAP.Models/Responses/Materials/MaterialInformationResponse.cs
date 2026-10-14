using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Responses.Materials;

public sealed class MaterialInformationResponse
{
    public string MaterialLot { get; set; } = string.Empty;
    public string? MaterialPn { get; set; }
    public decimal? Quantity { get; set; }
    public DateTime? ExpireTime { get; set; }
}
