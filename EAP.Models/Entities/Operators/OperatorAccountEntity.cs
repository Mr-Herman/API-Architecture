namespace EAP.Models.Entities.Operators;

public sealed class OperatorAccountEntity
{
    public string OperatorId { get; set; } = string.Empty;

    public string? Password { get; set; }

    public string? OperatorName { get; set; }

    public string? Roles { get; set; }
}
