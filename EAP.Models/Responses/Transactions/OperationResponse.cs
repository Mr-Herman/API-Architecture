namespace EAP.Models.Responses.Transactions;

public sealed class OperationResponse
{
    public string TransactionId { get; set; } = string.Empty;

    public int AffectedCount { get; set; }
}
