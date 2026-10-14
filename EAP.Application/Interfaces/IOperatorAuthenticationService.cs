using EAP.Models.Common;
using EAP.Models.Requests.Operators;
using EAP.Models.Responses.Operators;
using EAP.Models.Responses.Transactions;

namespace EAP.Application.Interfaces;

public interface IOperatorAuthenticationService
{
    Task<ApiResult<OperatorAuthenticationResponse>> AuthenticateAsync(
        AuthenticateOperatorRequest request,
        CancellationToken cancellationToken);

    Task<ApiResult<OperatorAuthenticationResponse>> ValidateCredentialsAsync(
        ValidateOperatorCredentialsRequest request,
        CancellationToken cancellationToken);

    Task<ApiResult<EquipmentAvailabilityResponse>> ValidateEquipmentAvailabilityAsync(
        ValidateEquipmentAvailabilityRequest request,
        CancellationToken cancellationToken);

    Task<ApiResult<OperatorProcessValidationResponse>> ValidateQualificationAsync(
        ValidateOperatorQualificationRequest request,
        CancellationToken cancellationToken);

    Task<ApiResult<OperationResponse>> LoginOperatorAsync(
        LoginOperatorRequest request,
        CancellationToken cancellationToken);
}
