using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Responses.Production;

public sealed class ProcessDefectResponse
{
    public string? WorkFlow { get; set; }
    public string? Process { get; set; }
    public string? DefectCode { get; set; }
    public string? DefectName { get; set; }
    public string? FieldName { get; set; }
    public string? TableName { get; set; }
}
