using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Responses.Operators;

public sealed class OperatorProcessValidationResponse
{
    public bool Authenticated { get; set; }
    public string? FailureReason { get; set; }
}
