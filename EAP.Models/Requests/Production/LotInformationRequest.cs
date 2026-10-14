using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Production;

public sealed class LotInformationRequest
{
    [Required, StringLength(100)]
    public string LotIdOrSn { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;

    public string? WorkFlow { get; set; }

    public string? Process { get; set; }

    public string? EmployeeId { get; set; }
}
