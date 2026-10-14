namespace EAP.Models.Entities.Operators;

public sealed class OperatorProcessEntity
{
    public string? WorkFlow { get; set; }
    public string? Process { get; set; }
    public IReadOnlyList<string> Operators { get; set; } = [];
}
