using Dapper;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;

namespace EAP.DataAccess.Repositories.Commands;

public sealed class MaterialCommandRepository : RepositoryBase, IMaterialCommandRepository
{
    private const string DatabaseName = "DgFisOracle";

    public MaterialCommandRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName)
    {
    }

    public Task<int> ReplaceSetupAsync(MaterialSetupRequest request, CancellationToken cancellationToken)
    {
        return ExecuteInTransactionAsync(
            async (connection, transaction, token) =>
            {
                var lots = request.Materials
                    .Select(item => item.MaterialLot.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var lotParameters = new DynamicParameters();
                var lotNames = new List<string>();
                for (var index = 0; index < lots.Length; index++)
                {
                    var parameterName = $"Lot{index}";
                    lotNames.Add($":{parameterName}");
                    lotParameters.Add(parameterName, lots[index]);
                }

                var duplicateSql = $@"
                    SELECT MATERIAL_LOT
                    FROM EQUIPMENT_MATERIAL_WIP
                    WHERE MATERIAL_LOT IN ({string.Join(", ", lotNames)})
                    ";

                var duplicateCommand = new CommandDefinition(
                    duplicateSql,
                    lotParameters,
                    transaction,
                    CommandTimeoutSeconds,
                    cancellationToken: token);
                var duplicates = (await connection.QueryAsync<string>(duplicateCommand)).AsList();

                if (duplicates.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"Material lots already exist in WIP: {string.Join(", ", duplicates)}.");
                }

                string deleteSql = @"
                    DELETE FROM EQUIPMENT_MATERIAL_WIP
                    WHERE EQUIPMENT_ID = :EquipmentId
                    ";
                var deleteCommand = new CommandDefinition(
                    deleteSql,
                    new { request.EquipmentId },
                    transaction,
                    CommandTimeoutSeconds,
                    cancellationToken: token);
                var affected = await connection.ExecuteAsync(deleteCommand);

                string insertSql = @"
                    INSERT INTO EQUIPMENT_MATERIAL_WIP
                        (EQUIPMENT_ID, COMPONENT_NAME, MATERIAL_LOT, MATERIAL_PN,
                         ORIGINAL_QTY, REMAIN_QTY, EXPIRE_TIME, CREATE_USER, LOAD_TIME)
                    VALUES
                        (:EquipmentId, :ComponentName, :MaterialLot, :MaterialPn,
                         :OriginalQuantity, :RemainQuantity, :ExpireTime, :CreateUser, :TransactionTime)
                    ";

                foreach (var item in request.Materials)
                {
                    var insertCommand = new CommandDefinition(
                        insertSql,
                        new
                        {
                            request.EquipmentId,
                            item.ComponentName,
                            item.MaterialLot,
                            item.MaterialPn,
                            item.OriginalQuantity,
                            item.RemainQuantity,
                            item.ExpireTime,
                            CreateUser = request.TransactionId,
                            TransactionTime = request.TransactionTime.DateTime
                        },
                        transaction,
                        CommandTimeoutSeconds,
                        cancellationToken: token);

                    affected += await connection.ExecuteAsync(insertCommand);
                }

                return affected;
            },
            cancellationToken);
    }
}
