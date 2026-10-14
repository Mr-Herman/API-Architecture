using Dapper;
using EAP.Application.Interfaces.Commands;
using EAP.Models.Requests.Transactions;
using EAP.Models.Responses.Transactions;
using EAP.Models.Requests.Tools;
using EAP.Models.Responses.Tools;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;

namespace EAP.DataAccess.Repositories.Commands;

public sealed class ToolCommandRepository : RepositoryBase, IToolCommandRepository
{
    private const string DatabaseName = "DgFisOracle";

    public ToolCommandRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, DatabaseName)
    {
    }

    public Task<int> InsertSetupAsync(ToolSetupRequest request, CancellationToken cancellationToken)
    {
        return ExecuteInTransactionAsync(
            async (connection, transaction, token) =>
            {
                var affected = 0;
                string sql = @"
                    INSERT INTO TEST_ENV_TOOLSETUP
                        (EQUIPMENT_ID, OPERATOR_ID, TOOL_ID, LIFE_TIMES,
                         EXPIRE_TIME, TOOL_PN, TOOL_TYPE, TOOL_TYPE_NAME)
                    VALUES
                        (:EquipmentId, :OperatorId, :ToolId, :LifeTimes,
                         :ExpireTime, :ToolPn, :ToolType, :ToolTypeName)
                    ";

                foreach (var tool in request.Tools)
                {
                    var command = new CommandDefinition(
                        sql,
                        new
                        {
                            request.EquipmentId,
                            request.OperatorId,
                            tool.ToolId,
                            tool.LifeTimes,
                            tool.ExpireTime,
                            tool.ToolPn,
                            tool.ToolType,
                            tool.ToolTypeName
                        },
                        transaction,
                        CommandTimeoutSeconds,
                        cancellationToken: token);

                    affected += await connection.ExecuteAsync(command);
                }

                return affected;
            },
            cancellationToken);
    }

    public Task<ToolVerificationResponse> VerifyAsync(VerifyToolRequest request, CancellationToken cancellationToken)
    {
        string sql = @"
            SELECT STATUS AS Status, LIFE_TIMES AS LifeTimes
            FROM EQUIPMENT_TOOL_WIP
            WHERE EQUIPMENT_ID = :EquipmentId
              AND TOOL_ID = :ToolId
            ";

        return ExecuteWithConnectionAsync(
            async (connection, token) =>
            {
                var row = await connection.QueryFirstOrDefaultAsync<ToolVerificationRow>(CreateCommand(sql, request, token));
                if (row is null)
                {
                    return new ToolVerificationResponse
                    {
                        Passed = false,
                        Message = "ToolInformationNotFound"
                    };
                }

                if (string.Equals(row.Status, "Update", StringComparison.OrdinalIgnoreCase))
                {
                    if (request.ToolUsage != 0)
                    {
                        return new ToolVerificationResponse
                        {
                            Passed = false,
                            Message = "ToolMustBeReset"
                        };
                    }

                    string updateSql = @"
                        UPDATE EQUIPMENT_TOOL_WIP
                        SET STATUS = 'Verified'
                        WHERE EQUIPMENT_ID = :EquipmentId
                          AND TOOL_ID = :ToolId
                        ";
                    await connection.ExecuteAsync(CreateCommand(updateSql, request, token));
                }

                if (row.LifeTimes is not null && row.LifeTimes <= request.ToolUsage)
                {
                    return new ToolVerificationResponse
                    {
                        Passed = false,
                        Message = "ToolLifeReached"
                    };
                }

                return new ToolVerificationResponse
                {
                    Passed = true,
                    Message = "ToolVerificationPassed"
                };
            },
            cancellationToken);
    }

    private sealed class ToolVerificationRow
    {
        public string? Status { get; set; }

        public decimal? LifeTimes { get; set; }

    }

}
