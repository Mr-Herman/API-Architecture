namespace EAP.Models.Responses.Operators;

public sealed class OperatorAuthenticationResponse
{
    public bool Authenticated { get; set; }

    public string? OperatorName { get; set; }

    public string? Roles { get; set; }

    public string? FailureReason { get; set; }
}
