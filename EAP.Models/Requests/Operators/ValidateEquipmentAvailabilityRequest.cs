using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Operators;

public sealed class ValidateEquipmentAvailabilityRequest
{
    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;
}
