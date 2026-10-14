using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Carriers;

public sealed class GetCarrierSetupRequest
{
    [Required, StringLength(50)]
    public string CarrierId { get; set; } = string.Empty;
}
