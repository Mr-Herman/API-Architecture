using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Materials;

public sealed class MaterialInformationRequest
{
    [Required, StringLength(100)]
    public string MaterialLot { get; set; } = string.Empty;
}
