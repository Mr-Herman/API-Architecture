using EAP.Models.Requests.Operators;
using EAP.Models.Requests.Transactions;

namespace EAP.Application.Mappings;

internal static class OperatorAuthenticationRequestMapper
{
    public static ValidateOperatorCredentialsRequest ToCredentialsRequest(AuthenticateOperatorRequest request)
    {
        return new ValidateOperatorCredentialsRequest
        {
            OperatorId = request.OperatorId,
            Password = request.Password
        };
    }

    public static ValidateEquipmentAvailabilityRequest ToEquipmentRequest(AuthenticateOperatorRequest request)
    {
        return new ValidateEquipmentAvailabilityRequest
        {
            EquipmentId = request.EquipmentId
        };
    }

    public static ValidateOperatorQualificationRequest ToQualificationRequest(AuthenticateOperatorRequest request)
    {
        return new ValidateOperatorQualificationRequest
        {
            OperatorId = request.OperatorId,
            WorkFlow = request.WorkFlow,
            Process = request.Process
        };
    }

    public static LoginOperatorRequest ToLoginRequest(AuthenticateOperatorRequest request)
    {
        return new LoginOperatorRequest
        {
            OperatorId = request.OperatorId,
            EquipmentId = request.EquipmentId,
            WorkFlow = request.WorkFlow,
            Process = request.Process
        };
    }

    public static OperatorBindingRequest ToBindingRequest(LoginOperatorRequest request)
    {
        return new OperatorBindingRequest
        {
            OperatorId = request.OperatorId.Trim(),
            EquipmentId = request.EquipmentId.Trim(),
            WorkFlow = request.WorkFlow.Trim(),
            Process = request.Process.Trim()
        };
    }
}
