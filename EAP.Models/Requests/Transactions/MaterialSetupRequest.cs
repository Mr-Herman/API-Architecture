using System.ComponentModel.DataAnnotations;

using EAP.Models.Common;

namespace EAP.Models.Requests.Transactions;

public sealed class MaterialSetupRequest : ITransactionRequest
{
    [Required, StringLength(50)]
    public string EquipmentId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string TransactionId { get; set; } = string.Empty;

    public DateTimeOffset TransactionTime { get; set; }

    [Required]
    [MinLength(1)]
    public IReadOnlyList<Item> Materials { get; set; } = [];

    public sealed class Item
    {
        [Required]
        public string ComponentName { get; set; } = string.Empty;

        [Required]
        public string MaterialLot { get; set; } = string.Empty;

        public string? MaterialPn { get; set; }
        public decimal OriginalQuantity { get; set; }
        public decimal RemainQuantity { get; set; }
        public DateTime? ExpireTime { get; set; }
    }
}
