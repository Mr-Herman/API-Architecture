using EAP.Application.Interfaces;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;
using EAP.Models.Entities.Materials;

namespace EAP.DataAccess.Repositories;

public sealed class MaterialQueryRepository : RepositoryBase, IMaterialQueryRepository
{
    private const string DatabaseName = "DgFisOracle";

    public MaterialQueryRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName)
    {
    }

    public async Task<IReadOnlyList<EquipmentMaterialEntity>> GetEquipmentSetupAsync(string equipmentId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT
                EQUIPMENT_ID AS EquipmentId,
                COMPONENT_NAME AS ComponentName,
                MATERIAL_LOT AS MaterialLot,
                MATERIAL_PN AS MaterialPn,
                ORIGINAL_QTY AS OriginalQuantity,
                REMAIN_QTY AS RemainQuantity,
                EXPIRE_TIME AS ExpireTime
            FROM EQUIPMENT_MATERIAL_WIP
            WHERE EQUIPMENT_ID = :EquipmentId
            ORDER BY MATERIAL_LOT
            ";

        return await QueryListAsync<EquipmentMaterialEntity>(
            sql,
            new { EquipmentId = equipmentId },
            cancellationToken);
    }

    public async Task<IReadOnlyList<MaterialInformationEntity>> GetInformationAsync(string materialLot, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT
                MATERIAL_LOT AS MaterialLot,
                MATERIAL_PN AS MaterialPn,
                QTY AS Quantity,
                EXPIRE_TIME AS ExpireTime
            FROM MATERIAL_BATCH_INFO
            WHERE MATERIAL_LOT = :MaterialLot
            ";

        return await QueryListAsync<MaterialInformationEntity>(
            sql,
            new { MaterialLot = materialLot },
            cancellationToken);
    }
}
