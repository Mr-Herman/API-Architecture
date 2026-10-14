using EAP.Models.Entities.Operators;
using EAP.Models.Responses.Operators;

namespace EAP.Application.Mappings;

internal static class OperatorResponseMapper
{
    public static OperatorProcessResponse ToResponse(OperatorProcessEntity entity)
    {
        return new OperatorProcessResponse
        {
            WorkFlow = entity.WorkFlow,
            Process = entity.Process,
            Operators = entity.Operators
        };
    }
}
