using System.ComponentModel.DataAnnotations;
using EAP.Models.Common;

namespace EAP.Models.Requests.Production;

public sealed class TrackInRequest : ITransactionRequest
{
    [Required, StringLength(100)]
    public string TransactionId { get; set; } = string.Empty;

    public DateTime TransactionTime { get; set; }

    [Range(1, 2)]
    public int TraceWay { get; set; }

    [StringLength(100)]
    public string? LotId { get; set; }

    [StringLength(100)]
    public string? SerialNumber { get; set; }

    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string RecipeName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? WorkFlow { get; set; }

    [StringLength(100)]
    public string? Process { get; set; }

    [StringLength(50)]
    public string? EmployeeId { get; set; }
}
