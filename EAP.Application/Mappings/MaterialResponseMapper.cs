using EAP.Models.Entities.Materials;
using EAP.Models.Responses.Materials;

namespace EAP.Application.Mappings;

internal static class MaterialResponseMapper
{
    public static EquipmentMaterialResponse ToResponse(EquipmentMaterialEntity entity)
    {
        return new EquipmentMaterialResponse
        {
            EquipmentId = entity.EquipmentId,
            ComponentName = entity.ComponentName,
            MaterialLot = entity.MaterialLot,
            MaterialPn = entity.MaterialPn,
            OriginalQuantity = entity.OriginalQuantity,
            RemainQuantity = entity.RemainQuantity,
            ExpireTime = entity.ExpireTime
        };
    }

    public static MaterialInformationResponse ToResponse(MaterialInformationEntity entity)
    {
        return new MaterialInformationResponse
        {
            MaterialLot = entity.MaterialLot,
            MaterialPn = entity.MaterialPn,
            Quantity = entity.Quantity,
            ExpireTime = entity.ExpireTime
        };
    }
}
