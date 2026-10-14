using EAP.Models.Entities.Tools;
using EAP.Models.Responses.Tools;

namespace EAP.Application.Mappings;

internal static class ToolResponseMapper
{
    public static ToolLotInformationResponse ToResponse(ToolLotInformationEntity entity)
    {
        return new ToolLotInformationResponse
        {
            ToolId = entity.ToolId,
            EquipmentId = entity.EquipmentId,
            LifeTimes = entity.LifeTimes,
            LifeHours = entity.LifeHours,
            ToolPn = entity.ToolPn,
            ToolType = entity.ToolType,
            ToolTypeName = entity.ToolTypeName
        };
    }

    public static EquipmentToolResponse ToResponse(EquipmentToolEntity entity)
    {
        return new EquipmentToolResponse
        {
            ToolId = entity.ToolId,
            LifeTimes = entity.LifeTimes,
            ExpireTime = entity.ExpireTime,
            ToolPn = entity.ToolPn,
            ToolType = entity.ToolType,
            ToolTypeName = entity.ToolTypeName
        };
    }
}
