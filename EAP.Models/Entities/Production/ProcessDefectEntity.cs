namespace EAP.Models.Entities.Production;

public sealed class ProcessDefectEntity
{
    public string? WorkFlow { get; set; }
    public string? Process { get; set; }
    public string? DefectCode { get; set; }
    public string? DefectName { get; set; }
    public string? FieldName { get; set; }
    public string? TableName { get; set; }
}
