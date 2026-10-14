using EAP.Application.Interfaces;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;
using EAP.Models.Entities.Carriers;

namespace EAP.DataAccess.Repositories;

public sealed class CarrierQueryRepository : RepositoryBase, ICarrierQueryRepository
{
    private const string DatabaseName = "DgFisOracle";

    public CarrierQueryRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName)
    {
    }

    public async Task<CarrierSetupEntity?> GetSetupAsync(string carrierId, CancellationToken cancellationToken)
    {
        string carrierSql = @"
            SELECT
                a.CARRIER_ID AS CarrierId,
                a.CARRIER_PN AS CarrierPn,
                b.CARRIER_FAMILY AS CarrierFamily
            FROM CARRIER_SN_INFO a
            INNER JOIN CARRIER_PN_LIST b ON a.CARRIER_PN = b.CARRIER_PN
            WHERE a.CARRIER_ID = :CarrierId
            ";

        var carrier = await QueryFirstOrDefaultAsync<CarrierSetupEntity>(
            carrierSql,
            new { CarrierId = carrierId },
            cancellationToken);

        if (carrier is null)
        {
            return null;
        }

        string lotsSql = @"
            SELECT PRODUCTION_LOT_NO
            FROM PRODUCTION_CARRIER_WIP
            WHERE CARRIER_ID = :CarrierId
            ORDER BY PRODUCTION_LOT_NO
            ";

        var lots = await QueryListAsync<string>(
            lotsSql,
            new { CarrierId = carrierId },
            cancellationToken);

        carrier.Lots = lots;
        return carrier;
    }

    public async Task<string?> ResolveLotIdAsync(string lotIdOrSn, CancellationToken cancellationToken)
    {
        string lotSql = @"
            SELECT PRODUCTION_LOT_NO
            FROM PRODUCTION_LOT_MASTER
            WHERE PRODUCTION_LOT_NO = :LotIdOrSn
            ";

        var lotId = await QueryFirstOrDefaultAsync<string>(
            lotSql,
            new { LotIdOrSn = lotIdOrSn },
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(lotId))
        {
            return lotId.Trim();
        }

        string serialSql = @"
            SELECT PRODUCTION_LOT_NO
            FROM SN_MASTER
            WHERE KEY_MATERIAL_SN = :LotIdOrSn
            ";

        return await QueryFirstOrDefaultAsync<string>(
            serialSql,
            new { LotIdOrSn = lotIdOrSn },
            cancellationToken);
    }

    public async Task<bool> IsLotBoundAsync(string lotId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT COUNT(1)
            FROM PRODUCTION_CARRIER_WIP
            WHERE PRODUCTION_LOT_NO = :LotId
            ";

        var count = await QueryFirstOrDefaultAsync<int>(
            sql,
            new { LotId = lotId },
            cancellationToken);

        return count > 0;
    }

}
