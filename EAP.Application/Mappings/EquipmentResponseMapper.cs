using EAP.Models.Entities.Equipment;
using EAP.Models.Responses.Equipment;

namespace EAP.Application.Mappings;

internal static class EquipmentResponseMapper
{
    public static EquipmentStatusResponse ToResponse(EquipmentStatusEntity entity)
    {
        return new EquipmentStatusResponse
        {
            EquipmentId = entity.EquipmentId,
            Status = entity.Status,
            SubStatus = entity.SubStatus,
            SubStatusDesc = entity.SubStatusDesc
        };
    }

    public static EquipmentProcessResponse ToResponse(EquipmentProcessEntity entity)
    {
        return new EquipmentProcessResponse
        {
            WorkFlow = entity.WorkFlow,
            DisplayWorkFlow = entity.DisplayWorkFlow,
            Process = entity.Process,
            DisplayProcess = entity.DisplayProcess
        };
    }
}
