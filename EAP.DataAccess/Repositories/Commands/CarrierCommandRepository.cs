using Dapper;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;
using System.Data.Common;

namespace EAP.DataAccess.Repositories.Commands;

public sealed class CarrierCommandRepository : RepositoryBase, ICarrierCommandRepository
{
    private const string DatabaseName = "DgFisOracle";

    public CarrierCommandRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName)
    {
    }

    public Task<int> ConfirmLotSetupAsync(CarrierLotSetupRequest request, CancellationToken cancellationToken)
    {
        return ExecuteInTransactionAsync(
            (connection, transaction, token) =>
                ConfirmLotSetupInTransactionAsync(
                    connection,
                    transaction,
                    request,
                    token),
            cancellationToken);
    }

    private async Task<int> ConfirmLotSetupInTransactionAsync(DbConnection connection, DbTransaction transaction, CarrierLotSetupRequest request, CancellationToken cancellationToken)
    {
        var lots = GetDistinctLots(request);

        await EnsureLotsDoNotExistAsync(connection, transaction, lots, cancellationToken);

        return await InsertLotsAsync(connection, transaction, request, lots, cancellationToken);
    }

    private async Task EnsureLotsDoNotExistAsync(DbConnection connection, DbTransaction transaction, IReadOnlyList<string> lots, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        var names = new List<string>();

        for (var index = 0; index < lots.Count; index++)
        {
            var name = $"Lot{index}";
            names.Add($":{name}");
            parameters.Add(name, lots[index]);
        }

        var duplicateSql = $@"
            SELECT PRODUCTION_LOT_NO
            FROM PRODUCTION_CARRIER_WIP
            WHERE PRODUCTION_LOT_NO IN ({string.Join(", ", names)})
            ";
        var command = new CommandDefinition(
            duplicateSql,
            parameters,
            transaction,
            CommandTimeoutSeconds,
            cancellationToken: cancellationToken);
        var duplicates = (await connection.QueryAsync<string>(command)).AsList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException($"Lots already exist in WIP: {string.Join(", ", duplicates)}.");
        }
    }

    private async Task<int> InsertLotsAsync(DbConnection connection, DbTransaction transaction, CarrierLotSetupRequest request, IReadOnlyList<string> lots, CancellationToken cancellationToken)
    {
        string sql = @"
            INSERT INTO PRODUCTION_CARRIER_WIP
                (CARRIER_ID, PRODUCTION_LOT_NO, LOADING_TIME)
            VALUES
                (:CarrierId, :ProductionLotNo, :TransactionTime)
            ";
        var affected = 0;

        foreach (var lot in lots)
        {
            var command = new CommandDefinition(
                sql,
                new
                {
                    request.CarrierId,
                    ProductionLotNo = lot,
                    TransactionTime = request.TransactionTime.DateTime
                },
                transaction,
                CommandTimeoutSeconds,
                cancellationToken: cancellationToken);

            affected += await connection.ExecuteAsync(command);
        }

        return affected;
    }

    private static IReadOnlyList<string> GetDistinctLots(CarrierLotSetupRequest request)
    {
        return request.Lots
            .Select(lot => lot.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public Task<int> RemoveLotSetupAsync(RemoveCarrierLotSetupRequest request, CancellationToken cancellationToken)
    {
        return ExecuteInTransactionAsync(
            async (connection, transaction, token) =>
            {
                string selectSql = @"
                    SELECT LOADING_TIME
                    FROM PRODUCTION_CARRIER_WIP
                    WHERE CARRIER_ID = :CarrierId
                      AND PRODUCTION_LOT_NO = :LotId
                    ";
                var selectCommand = new CommandDefinition(
                    selectSql,
                    new { request.CarrierId, LotId = request.LotId },
                    transaction,
                    CommandTimeoutSeconds,
                    cancellationToken: token);
                var loadingTime = await connection.QueryFirstOrDefaultAsync<DateTime?>(selectCommand);

                if (loadingTime is null)
                {
                    return 0;
                }

                string deleteSql = @"
                    DELETE FROM PRODUCTION_CARRIER_WIP
                    WHERE CARRIER_ID = :CarrierId
                      AND PRODUCTION_LOT_NO = :LotId
                    ";
                var deleteCommand = new CommandDefinition(
                    deleteSql,
                    new { request.CarrierId, LotId = request.LotId },
                    transaction,
                    CommandTimeoutSeconds,
                    cancellationToken: token);
                var affected = await connection.ExecuteAsync(deleteCommand);

                string historySql = @"
                    INSERT INTO PRODUCTION_CARRIER_HIST
                        (HIST_SEQ, CARRIER_ID, PRODUCTION_LOT_NO, LOAD_TIME, UNLOAD_TIME)
                    VALUES
                        (PRODUCTION_CARRIER_HIST_SEQ.NEXTVAL, :CarrierId, :LotId, :LoadingTime, SYSDATE)
                    ";
                var historyCommand = new CommandDefinition(
                    historySql,
                    new
                    {
                        request.CarrierId,
                        LotId = request.LotId,
                        LoadingTime = loadingTime.Value
                    },
                    transaction,
                    CommandTimeoutSeconds,
                    cancellationToken: token);
                affected += await connection.ExecuteAsync(historyCommand);

                return affected;
            },
            cancellationToken);
    }
}
