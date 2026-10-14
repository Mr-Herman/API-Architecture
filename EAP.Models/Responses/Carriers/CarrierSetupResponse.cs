using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Responses.Carriers;

public sealed class CarrierSetupResponse
{
    public string CarrierId { get; set; } = string.Empty;
    public string? CarrierPn { get; set; }
    public string? CarrierFamily { get; set; }
    public IReadOnlyList<string> Lots { get; set; } = [];
}
