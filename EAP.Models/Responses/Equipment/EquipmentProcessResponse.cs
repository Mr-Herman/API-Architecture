using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Responses.Equipment;

public sealed class EquipmentProcessResponse
{
    public string? WorkFlow { get; set; }
    public string? DisplayWorkFlow { get; set; }
    public string? Process { get; set; }
    public string? DisplayProcess { get; set; }
}
