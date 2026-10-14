using EAP.Models.Entities.Carriers;
using EAP.Models.Responses.Carriers;

namespace EAP.Application.Mappings;

internal static class CarrierValidationResponseMapper
{
    public static ValidateLotCarrierResponse ToResponse(CarrierSetupEntity entity, string lotId)
    {
        return new ValidateLotCarrierResponse
        {
            CarrierId = entity.CarrierId,
            CarrierPn = entity.CarrierPn,
            CarrierFamily = entity.CarrierFamily,
            LotId = lotId
        };
    }
}
