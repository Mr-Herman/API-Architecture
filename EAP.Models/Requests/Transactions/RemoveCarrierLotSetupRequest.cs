using System.ComponentModel.DataAnnotations;

using EAP.Models.Common;

namespace EAP.Models.Requests.Transactions;

public sealed class RemoveCarrierLotSetupRequest : ITransactionRequest
{
    [Required, StringLength(100)]
    public string TransactionId { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string CarrierId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LotId { get; set; } = string.Empty;

    public DateTimeOffset TransactionTime { get; set; }
}
