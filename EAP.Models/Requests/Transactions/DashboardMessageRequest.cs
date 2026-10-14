using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Transactions;

public sealed class DashboardMessageRequest
{
    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;

    [StringLength(100)]
    public string? LotId { get; set; }

    [Required, StringLength(30)]
    public string Severity { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string AlertType { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    public DateTimeOffset EventTime { get; set; }

    [Required, StringLength(50)]
    public string OperatorId { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string WorkFlow { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Process { get; set; } = string.Empty;
}
