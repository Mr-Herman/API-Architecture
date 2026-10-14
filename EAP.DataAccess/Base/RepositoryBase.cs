using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using Dapper;
using EAP.Application.Logging;
using EAP.DataAccess.Database;

namespace EAP.DataAccess.Base;

public abstract class RepositoryBase
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly string _connectionName;
    private readonly int _commandTimeoutSeconds;

    protected RepositoryBase(IDbConnectionFactory connectionFactory, string connectionName)
    {
        _connectionFactory = connectionFactory;
        _connectionName = connectionName;
        _commandTimeoutSeconds = connectionFactory.GetCommandTimeoutSeconds(connectionName);
    }

    protected async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters, CancellationToken cancellationToken, [CallerMemberName] string operation = "")
    {
        using var timing = StartTiming(operation);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var command = CreateCommand(sql, parameters, cancellationToken);

        return await connection.QueryFirstOrDefaultAsync<T>(command);
    }

    protected async Task<T> QuerySingleAsync<T>(string sql, object? parameters, CancellationToken cancellationToken, [CallerMemberName] string operation = "")
    {
        using var timing = StartTiming(operation);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var command = CreateCommand(sql, parameters, cancellationToken);

        return await connection.QuerySingleAsync<T>(command);
    }

    protected async Task<IReadOnlyList<T>> QueryListAsync<T>(string sql, object? parameters, CancellationToken cancellationToken, [CallerMemberName] string operation = "")
    {
        using var timing = StartTiming(operation);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var command = CreateCommand(sql, parameters, cancellationToken);
        var result = await connection.QueryAsync<T>(command);

        return result.AsList();
    }

    protected async Task<int> ExecuteAsync(string sql, object? parameters, CancellationToken cancellationToken, [CallerMemberName] string operation = "")
    {
        using var timing = StartTiming(operation);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var command = CreateCommand(sql, parameters, cancellationToken);

        return await connection.ExecuteAsync(command);
    }

    protected async Task<T?> QueryFirstOrDefaultStoredProcedureAsync<T>(string procedureName, object? parameters, CancellationToken cancellationToken, [CallerMemberName] string operation = "")
    {
        using var timing = StartTiming(operation);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var command = CreateCommand(
            procedureName,
            parameters,
            cancellationToken,
            commandType: CommandType.StoredProcedure);

        return await connection.QueryFirstOrDefaultAsync<T>(command);
    }

    protected async Task<T> QuerySingleStoredProcedureAsync<T>(string procedureName, object? parameters, CancellationToken cancellationToken, [CallerMemberName] string operation = "")
    {
        using var timing = StartTiming(operation);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var command = CreateCommand(
            procedureName,
            parameters,
            cancellationToken,
            commandType: CommandType.StoredProcedure);

        return await connection.QuerySingleAsync<T>(command);
    }

    protected async Task<IReadOnlyList<T>> QueryStoredProcedureAsync<T>(string procedureName, object? parameters, CancellationToken cancellationToken, [CallerMemberName] string operation = "")
    {
        using var timing = StartTiming(operation);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var command = CreateCommand(
            procedureName,
            parameters,
            cancellationToken,
            commandType: CommandType.StoredProcedure);
        var result = await connection.QueryAsync<T>(command);

        return result.AsList();
    }

    protected async Task<int> ExecuteStoredProcedureAsync(string procedureName, object? parameters, CancellationToken cancellationToken, [CallerMemberName] string operation = "")
    {
        using var timing = StartTiming(operation);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var command = CreateCommand(
            procedureName,
            parameters,
            cancellationToken,
            commandType: CommandType.StoredProcedure);

        return await connection.ExecuteAsync(command);
    }

    protected async Task<T> ExecuteWithConnectionAsync<T>(Func<DbConnection, CancellationToken, Task<T>> action, CancellationToken cancellationToken, [CallerMemberName] string operation = "")
    {
        ArgumentNullException.ThrowIfNull(action);

        using var timing = StartTiming(operation);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        return await action(connection, cancellationToken);
    }

    protected async Task ExecuteWithConnectionAsync(Func<DbConnection, CancellationToken, Task> action, CancellationToken cancellationToken, [CallerMemberName] string operation = "")
    {
        ArgumentNullException.ThrowIfNull(action);

        using var timing = StartTiming(operation);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await action(connection, cancellationToken);
    }

    protected async Task<T> ExecuteInTransactionAsync<T>(Func<DbConnection, DbTransaction, CancellationToken, Task<T>> action, CancellationToken cancellationToken, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, [CallerMemberName] string operation = "")
    {
        ArgumentNullException.ThrowIfNull(action);

        using var timing = StartTiming(operation);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(isolationLevel, cancellationToken);

        try
        {
            var result = await action(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                throw new AggregateException("Database operation and transaction rollback both failed.", exception, rollbackException);
            }

            throw;
        }
    }

    protected async Task ExecuteInTransactionAsync(Func<DbConnection, DbTransaction, CancellationToken, Task> action, CancellationToken cancellationToken, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, [CallerMemberName] string operation = "")
    {
        ArgumentNullException.ThrowIfNull(action);

        await ExecuteInTransactionAsync(
            async (connection, transaction, token) =>
            {
                await action(connection, transaction, token);
                return true;
            },
            cancellationToken,
            isolationLevel,
            operation);
    }

    protected CommandDefinition CreateCommand(string commandText, object? parameters, CancellationToken cancellationToken, DbTransaction? transaction = null, CommandType commandType = CommandType.Text)
    {
        return new CommandDefinition(
            commandText,
            parameters,
            transaction,
            _commandTimeoutSeconds,
            commandType,
            cancellationToken: cancellationToken);
    }

    protected int CommandTimeoutSeconds => _commandTimeoutSeconds;

    protected MethodTimingScope StartTiming([CallerMemberName] string operation = "")
    {
        return MethodTimingScope.Start("Database", operation, _connectionName);
    }

    protected Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        return _connectionFactory.OpenConnectionAsync(_connectionName, cancellationToken);
    }
}
