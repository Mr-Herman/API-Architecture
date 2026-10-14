using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Operators;

public sealed class ValidateOperatorCredentialsRequest
{
    [Required, StringLength(50)]
    public string OperatorId { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Password { get; set; } = string.Empty;
}
