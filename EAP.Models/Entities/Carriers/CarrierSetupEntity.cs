namespace EAP.Models.Entities.Carriers;

public sealed class CarrierSetupEntity
{
    public string CarrierId { get; set; } = string.Empty;
    public string? CarrierPn { get; set; }
    public string? CarrierFamily { get; set; }
    public IReadOnlyList<string> Lots { get; set; } = [];
}
