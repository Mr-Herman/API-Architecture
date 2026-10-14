using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Production;

public sealed class ProcessDefectRequest
{
    [Required, StringLength(50)]
    public string WorkFlow { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Process { get; set; } = string.Empty;
}
