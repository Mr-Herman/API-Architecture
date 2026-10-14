using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Responses.Tools;

public sealed class ToolVerificationResponse
{
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
}
