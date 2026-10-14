using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Responses.Operators;

public sealed class OperatorProcessResponse
{
    public string? WorkFlow { get; set; }
    public string? Process { get; set; }
    public IReadOnlyList<string> Operators { get; set; } = [];
}
