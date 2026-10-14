using EAP.Models.Entities.Operators;

namespace EAP.Application.Interfaces;

public interface IOperatorQueryRepository
{
    Task<OperatorAccountEntity?> GetOperatorAccountAsync(string operatorId, CancellationToken cancellationToken);

    Task<OperatorProcessEntity?> GetProcessOperatorsAsync(string equipmentId, CancellationToken cancellationToken);

    Task<bool> HasWorkInProgressAsync(string equipmentId, string operatorId, CancellationToken cancellationToken);

    Task<ProcessDefinitionEntity?> GetProcessDefinitionAsync(string workFlow, string process, CancellationToken cancellationToken);
}
