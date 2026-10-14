using EAP.Models.Common;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Mappings;

internal static class OperationResponseMapper
{
    public static OperationResponse ToResponse(int affectedCount)
    {
        return new OperationResponse
        {
            AffectedCount = affectedCount
        };
    }

    public static OperationResponse ToResponse(ITransactionRequest request, int affectedCount)
    {
        return new OperationResponse
        {
            TransactionId = request.TransactionId,
            AffectedCount = affectedCount
        };
    }
}
