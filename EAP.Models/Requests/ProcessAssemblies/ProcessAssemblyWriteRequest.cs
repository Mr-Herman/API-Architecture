using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.ProcessAssemblies;

public abstract class ProcessAssemblyWriteRequest
{
    [Required, StringLength(50)]
    public string JobNum { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string PartNo { get; set; } = string.Empty;

    [StringLength(50)]
    public string? ProjCode { get; set; }

    [StringLength(80)]
    public string? ProductCode { get; set; }

    [Required, StringLength(50)]
    public string ProcessId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string ProductionLotNo { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Marking { get; set; }

    [StringLength(50)]
    public string? Tester { get; set; }

    [StringLength(20)]
    public string? OperatorId { get; set; }

    [Range(0, int.MaxValue)]
    public int? InputQty { get; set; }

    [Range(0, int.MaxValue)]
    public int? OutputQty { get; set; }

    public DateTime? StartTime { get; set; }
    public DateTime? TestTime { get; set; }

    [StringLength(100)]
    public string? Remark { get; set; }

}
