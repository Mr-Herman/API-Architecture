using EAP.Application.Interfaces;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Common;
using EAP.Models.Requests.Equipment;
using EAP.Models.Responses.Equipment;
using EAP.Models.Requests.Operators;
using EAP.Models.Responses.Operators;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace EAP.Api.Controllers;

[ApiController]
[Route("fis/Operator")]
public sealed class OperatorsController : ControllerBase
{
    private readonly IOperatorQueryService _service;
    private readonly IOperatorAuthenticationService _authenticationService;
    private readonly IEquipmentCommandService _commandService;

    public OperatorsController(IOperatorQueryService service, IOperatorAuthenticationService authenticationService, IEquipmentCommandService commandService)
    {
        _service = service;
        _authenticationService = authenticationService;
        _commandService = commandService;
    }

    [HttpPost("AuthenticateOperator")]
    public Task<ApiResult<OperatorAuthenticationResponse>> AuthenticateOperator([FromBody] AuthenticateOperatorRequest request, CancellationToken cancellationToken)
    {
        return _authenticationService.AuthenticateAsync(request, cancellationToken);
    }

    [HttpPost("ValidateOperatorCredentials")]
    public Task<ApiResult<OperatorAuthenticationResponse>> ValidateOperatorCredentials([FromBody] ValidateOperatorCredentialsRequest request, CancellationToken cancellationToken)
    {
        return _authenticationService.ValidateCredentialsAsync(request, cancellationToken);
    }

    [HttpPost("ValidateEquipmentAvailability")]
    public Task<ApiResult<EquipmentAvailabilityResponse>> ValidateEquipmentAvailability([FromBody] ValidateEquipmentAvailabilityRequest request, CancellationToken cancellationToken)
    {
        return _authenticationService.ValidateEquipmentAvailabilityAsync(request, cancellationToken);
    }

    [HttpPost("ValidateOperatorQualification")]
    public Task<ApiResult<OperatorProcessValidationResponse>> ValidateOperatorQualification([FromBody] ValidateOperatorQualificationRequest request, CancellationToken cancellationToken)
    {
        return _authenticationService.ValidateQualificationAsync(request, cancellationToken);
    }

    [HttpPost("LoginOperator")]
    public Task<ApiResult<OperationResponse>> LoginOperator([FromBody] LoginOperatorRequest request, CancellationToken cancellationToken)
    {
        return _authenticationService.LoginOperatorAsync(request, cancellationToken);
    }

    [HttpPost("GetProcOpr")]
    public Task<ApiResult<OperatorProcessResponse>> GetEquipmentProcess([FromBody] OperatorEquipmentQueryRequest request, CancellationToken cancellationToken)
    {
        return _service.GetProcessOperatorsAsync(request, cancellationToken);
    }

    [HttpPost("ValidateProcOpr")]
    public Task<ApiResult<OperatorProcessValidationResponse>> ValidateProcess([FromBody] ValidateOperatorProcessRequest request, CancellationToken cancellationToken)
    {
        return _service.ValidateProcessAsync(request, cancellationToken);
    }

    [HttpPost("ConfirmProcOpr")]
    public Task<ApiResult<OperationResponse>> ConfirmBinding([FromBody] OperatorBindingRequest request, CancellationToken cancellationToken)
    {
        return _commandService.ConfirmOperatorAsync(request, cancellationToken);
    }

    [HttpPost("RemoveProcOpr")]
    public Task<ApiResult<OperationResponse>> RemoveBinding([FromBody] RemoveOperatorBindingRequest request, CancellationToken cancellationToken)
    {
        return _commandService.RemoveOperatorAsync(request, cancellationToken);
    }

    [HttpPost("SimulateOA")]
    public Task<ApiResult<OperationResponse>> SimulateOa([FromBody] EquipmentStatusChangeRequest request, CancellationToken cancellationToken)
    {
        return _commandService.ChangeStatusAsync(request, cancellationToken);
    }
}
