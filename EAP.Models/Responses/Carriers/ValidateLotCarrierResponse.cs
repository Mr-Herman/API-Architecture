namespace EAP.Models.Responses.Carriers;

public sealed class ValidateLotCarrierResponse
{
    public string CarrierId { get; set; } = string.Empty;
    public string? CarrierPn { get; set; }
    public string? CarrierFamily { get; set; }
    public string LotId { get; set; } = string.Empty;
}
