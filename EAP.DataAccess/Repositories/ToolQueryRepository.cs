using EAP.Application.Interfaces;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;
using EAP.Models.Entities.Tools;

namespace EAP.DataAccess.Repositories;

public sealed class ToolQueryRepository : RepositoryBase, IToolQueryRepository
{
    private const string DatabaseName = "DgFisOracle";

    public ToolQueryRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName)
    {
    }

    public async Task<ToolLotInformationEntity?> GetLotInformationAsync(string toolId, string equipmentId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT
                a.TOOL_ID AS ToolId,
                a.EQUIPMENT_ID AS EquipmentId,
                a.LIFE_TIMES AS LifeTimes,
                b.LIFE_HOURS AS LifeHours,
                a.TOOL_PN AS ToolPn,
                a.TOOL_TYPE AS ToolType,
                c.TOOL_TYPE_NAME AS ToolTypeName
            FROM EQUIPMENT_TOOL_WIP a
            INNER JOIN TOOL_PN_LIST b ON a.TOOL_PN = b.TOOL_PN
            INNER JOIN TOOL_TYPE_LIST c ON a.TOOL_TYPE = c.TOOL_TYPE
            WHERE a.EQUIPMENT_ID = :EquipmentId
              AND a.TOOL_ID = :ToolId
            ";

        return await QueryFirstOrDefaultAsync<ToolLotInformationEntity>(
            sql,
            new { EquipmentId = equipmentId, ToolId = toolId },
            cancellationToken);
    }

    public async Task<IReadOnlyList<EquipmentToolEntity>> GetEquipmentSetupAsync(string equipmentId, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT
                a.TOOL_ID AS ToolId,
                a.LIFE_TIMES AS LifeTimes,
                a.EXPIRE_TIME AS ExpireTime,
                a.TOOL_PN AS ToolPn,
                a.TOOL_TYPE AS ToolType,
                c.TOOL_TYPE_NAME AS ToolTypeName
            FROM EQUIPMENT_TOOL_WIP a
            INNER JOIN TOOL_PN_LIST b ON a.TOOL_PN = b.TOOL_PN
            INNER JOIN TOOL_TYPE_LIST c ON a.TOOL_TYPE = c.TOOL_TYPE
            WHERE a.EQUIPMENT_ID = :EquipmentId
            ORDER BY a.TOOL_ID
            ";

        return await QueryListAsync<EquipmentToolEntity>(
            sql,
            new { EquipmentId = equipmentId },
            cancellationToken);
    }
}
