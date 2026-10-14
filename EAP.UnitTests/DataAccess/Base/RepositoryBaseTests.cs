using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using EAP.DataAccess.Base;
using EAP.DataAccess.Database;

namespace EAP.UnitTests.Tests;

public sealed class RepositoryBaseTests
{
    [Fact]
    public async Task ExecuteInTransactionAsync_WhenSuccessful_CommitsAndDisposes()
    {
        var connection = new TestDbConnection();
        var repository = new TestRepository(new TestConnectionFactory(connection, commandTimeoutSeconds: 42));

        var result = await repository.ExecuteSuccessfulTransactionAsync();

        Assert.Equal("completed", result);
        Assert.Equal(1, connection.Transaction.CommitCount);
        Assert.Equal(0, connection.Transaction.RollbackCount);
        Assert.True(connection.IsDisposed);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationFails_RollsBackAndDisposes()
    {
        var connection = new TestDbConnection();
        var repository = new TestRepository(new TestConnectionFactory(connection, commandTimeoutSeconds: 42));

        await Assert.ThrowsAsync<InvalidOperationException>(repository.ExecuteFailingTransactionAsync);

        Assert.Equal(0, connection.Transaction.CommitCount);
        Assert.Equal(1, connection.Transaction.RollbackCount);
        Assert.True(connection.IsDisposed);
    }

    [Fact]
    public void CreateStoredProcedureCommand_UsesConfiguredTypeAndTimeout()
    {
        var connection = new TestDbConnection();
        var repository = new TestRepository(new TestConnectionFactory(connection, commandTimeoutSeconds: 42));

        var command = repository.CreateStoredProcedureCommand();

        Assert.Equal(CommandType.StoredProcedure, command.CommandType);
        Assert.Equal(42, command.CommandTimeout);
    }

    private sealed class TestRepository : RepositoryBase
    {
        public TestRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, "TestDatabase")
        {
        }

        public Task<string> ExecuteSuccessfulTransactionAsync()
        {
            return ExecuteInTransactionAsync(
                (_, _, _) => Task.FromResult("completed"),
                CancellationToken.None);
        }

        public Task<string> ExecuteFailingTransactionAsync()
        {
            return ExecuteInTransactionAsync<string>(
                (_, _, _) => Task.FromException<string>(
                    new InvalidOperationException("Database operation failed.")),
                CancellationToken.None);
        }

        public CommandDefinition CreateStoredProcedureCommand()
        {
            return CreateCommand("usp_Test", null, CancellationToken.None, commandType: CommandType.StoredProcedure);
        }
    }

    private sealed class TestConnectionFactory : IDbConnectionFactory
    {
        private readonly TestDbConnection _connection;
        private readonly int _commandTimeoutSeconds;

        public TestConnectionFactory(TestDbConnection connection, int commandTimeoutSeconds)
        {
            _connection = connection;
            _commandTimeoutSeconds = commandTimeoutSeconds;
        }

        public DbConnection CreateConnection(string connectionName)
        {
            return _connection;
        }

        public Task<DbConnection> OpenConnectionAsync(string connectionName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DbConnection>(_connection);
        }

        public int GetCommandTimeoutSeconds(string connectionName)
        {
            return _commandTimeoutSeconds;
        }
    }

    private sealed class TestDbConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Open;

        public TestDbTransaction Transaction { get; private set; } = null!;

        public bool IsDisposed { get; private set; }

        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "TestDatabase";

        public override string DataSource => "TestDataSource";

        public override string ServerVersion => "1.0";

        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName)
        {
        }

        public override void Close()
        {
            _state = ConnectionState.Closed;
        }

        public override void Open()
        {
            _state = ConnectionState.Open;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            Transaction = new TestDbTransaction(this, isolationLevel);
            return Transaction;
        }

        protected override ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(BeginDbTransaction(isolationLevel));
        }

        protected override DbCommand CreateDbCommand()
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            _state = ConnectionState.Closed;
            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestDbTransaction : DbTransaction
    {
        private readonly DbConnection _connection;

        public TestDbTransaction(DbConnection connection, IsolationLevel isolationLevel)
        {
            _connection = connection;
            IsolationLevel = isolationLevel;
        }

        public int CommitCount { get; private set; }

        public int RollbackCount { get; private set; }

        public override IsolationLevel IsolationLevel { get; }

        protected override DbConnection DbConnection => _connection;

        public override void Commit()
        {
            CommitCount++;
        }

        public override Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Commit();
            return Task.CompletedTask;
        }

        public override void Rollback()
        {
            RollbackCount++;
        }

        public override Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            Rollback();
            return Task.CompletedTask;
        }
    }
}
