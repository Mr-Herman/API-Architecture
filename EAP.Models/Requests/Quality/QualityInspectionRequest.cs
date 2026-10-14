using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Quality;

public sealed class QualityInspectionRequest
{
    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string InspectType { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Project { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string ProcessCode { get; set; } = string.Empty;
}
