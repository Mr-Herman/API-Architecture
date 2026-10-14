using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Carriers;

public sealed class ValidateLotCarrierRequest
{
    [Required, StringLength(50)]
    public string CarrierId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LotId { get; set; } = string.Empty;
}
