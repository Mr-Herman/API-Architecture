using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Transactions;

public sealed class ToolSetupRequest
{
    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string OperatorId { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public IReadOnlyList<Item> Tools { get; set; } = [];

    public sealed class Item
    {
        [Required]
        public string ToolId { get; set; } = string.Empty;

        public decimal? LifeTimes { get; set; }
        public DateTime? ExpireTime { get; set; }
        public string? ToolPn { get; set; }
        public string? ToolType { get; set; }
        public string? ToolTypeName { get; set; }
    }
}
