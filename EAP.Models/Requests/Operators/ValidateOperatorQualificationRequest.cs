using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Operators;

public sealed class ValidateOperatorQualificationRequest
{
    [Required, StringLength(50)]
    public string OperatorId { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string WorkFlow { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Process { get; set; } = string.Empty;
}
