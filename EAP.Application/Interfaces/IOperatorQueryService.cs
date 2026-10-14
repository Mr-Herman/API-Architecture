using EAP.Models.Common;
using EAP.Models.Requests.Operators;
using EAP.Models.Responses.Operators;

namespace EAP.Application.Interfaces;

public interface IOperatorQueryService
{
    Task<ApiResult<OperatorProcessResponse>> GetProcessOperatorsAsync(OperatorEquipmentQueryRequest request, CancellationToken cancellationToken);

    Task<ApiResult<OperatorProcessValidationResponse>> ValidateProcessAsync(ValidateOperatorProcessRequest request, CancellationToken cancellationToken);
}
