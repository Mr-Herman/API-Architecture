using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Transactions;

public sealed class RemoveOperatorBindingRequest
{
    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string OperatorId { get; set; } = string.Empty;
}
