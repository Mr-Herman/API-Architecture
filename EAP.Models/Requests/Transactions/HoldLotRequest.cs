using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EAP.Models.Requests.Transactions;

public sealed class HoldLotRequest
{
    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string HoldReason { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string OperatorId { get; set; } = string.Empty;

    [JsonIgnore]
    public string? WorkFlow { get; set; }

    [JsonIgnore]
    public string? Process { get; set; }

    [JsonIgnore]
    public decimal HoldNumber { get; set; }

    [Required]
    [MinLength(1)]
    public IReadOnlyList<Item> Items { get; set; } = [];

    public sealed class Item
    {
        [Required]
        public string LotId { get; set; } = string.Empty;

        public string? Sn { get; set; }
    }
}
