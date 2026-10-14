using EAP.Models.Entities.Carriers;

namespace EAP.Application.Interfaces;

public interface ICarrierQueryRepository
{
    Task<CarrierSetupEntity?> GetSetupAsync(string carrierId, CancellationToken cancellationToken);

    Task<string?> ResolveLotIdAsync(string lotIdOrSn, CancellationToken cancellationToken);

    Task<bool> IsLotBoundAsync(string lotId, CancellationToken cancellationToken);
}
