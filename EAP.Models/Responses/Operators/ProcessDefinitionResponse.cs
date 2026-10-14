using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Responses.Operators;

public sealed class ProcessDefinitionResponse
{
    public string? Product { get; set; }
    public string? ProcessName { get; set; }
    public string? ProcessCode { get; set; }
    public string? ProjectCode { get; set; }
}
