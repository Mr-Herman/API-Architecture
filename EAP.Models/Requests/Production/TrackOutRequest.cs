using System.ComponentModel.DataAnnotations;
using EAP.Models.Common;

namespace EAP.Models.Requests.Production;

public sealed class TrackOutRequest : ITransactionRequest
{
    [Required, StringLength(100)]
    public string TransactionId { get; set; } = string.Empty;

    public DateTime TransactionTime { get; set; }

    [Range(1, 2)]
    public int TraceWay { get; set; }

    [Required, StringLength(100)]
    public string LotId { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;

    [StringLength(100)]
    public string? WorkFlow { get; set; }

    [StringLength(100)]
    public string? Process { get; set; }

    [StringLength(50)]
    public string? EmployeeId { get; set; }

    public IReadOnlyList<SnResult> TrackOutSnResultList { get; set; } = [];

    public IReadOnlyList<LotResult> TrackOutLotResultList { get; set; } = [];

    public sealed class SnResult
    {
        [Required, StringLength(100)]
        public string SerialNumber { get; set; } = string.Empty;

        [Required, StringLength(10)]
        public string ProcessResult { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DefectCode { get; set; }

        [StringLength(200)]
        public string? DefectName { get; set; }

        [StringLength(500)]
        public string? TestDefect { get; set; }

        public int OnlineRework { get; set; }
    }

    public sealed class LotResult
    {
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required, StringLength(10)]
        public string ProcessResult { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DefectCode { get; set; }

        [StringLength(200)]
        public string? DefectName { get; set; }
    }
}
