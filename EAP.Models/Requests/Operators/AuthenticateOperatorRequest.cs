using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Operators;

public sealed class AuthenticateOperatorRequest
{
    [Required, StringLength(50)]
    public string OperatorId { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string WorkFlow { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Process { get; set; } = string.Empty;
}
