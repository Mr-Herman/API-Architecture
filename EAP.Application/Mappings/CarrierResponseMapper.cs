using EAP.Models.Entities.Carriers;
using EAP.Models.Responses.Carriers;

namespace EAP.Application.Mappings;

internal static class CarrierResponseMapper
{
    public static CarrierSetupResponse ToResponse(CarrierSetupEntity entity)
    {
        return new CarrierSetupResponse
        {
            CarrierId = entity.CarrierId,
            CarrierPn = entity.CarrierPn,
            CarrierFamily = entity.CarrierFamily,
            Lots = entity.Lots
        };
    }
}
