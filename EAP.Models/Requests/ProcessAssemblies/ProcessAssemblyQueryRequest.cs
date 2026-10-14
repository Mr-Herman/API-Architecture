using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.ProcessAssemblies;

public sealed class ProcessAssemblyQueryRequest
{
    [StringLength(50)]
    public string? JobNum { get; set; }

    [StringLength(50)]
    public string? PartNo { get; set; }

    [StringLength(50)]
    public string? ProcessId { get; set; }

    [StringLength(100)]
    public string? ProductionLotNo { get; set; }

    [StringLength(20)]
    public string? OperatorId { get; set; }

    public DateTime? StartTimeFrom { get; set; }
    public DateTime? StartTimeTo { get; set; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 200)]
    public int PageSize { get; set; } = 20;

}
