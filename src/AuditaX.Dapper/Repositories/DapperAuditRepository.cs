using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using AuditaX.Entities;
using AuditaX.Interfaces;

namespace AuditaX.Dapper.Repositories;

/// <summary>
/// Dapper-based implementation of the audit repository.
/// </summary>
/// <param name="dbConnection">The database connection.</param>
/// <param name="databaseProvider">The database provider.</param>
public sealed class DapperAuditRepository(
    IDbConnection dbConnection,
    IDatabaseProvider databaseProvider) : IAuditRepository
{
    private AuditLog? _pendingAuditLog;
    private bool _isNew;

    /// <inheritdoc />
    public async Task<AuditLog?> GetByEntityAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        EnsureConnectionOpen();

        var command = new CommandDefinition(
            databaseProvider.SelectByEntitySql,
            new { SourceName = sourceName, SourceKey = sourceKey },
            cancellationToken: cancellationToken);

        return await dbConnection.QueryFirstOrDefaultAsync<AuditLog>(command);
    }

    /// <inheritdoc />
    public async Task<AuditLog?> GetByEntityTrackingAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        var auditLog = await GetByEntityAsync(sourceName, sourceKey, cancellationToken);

        if (auditLog is not null)
        {
            _pendingAuditLog = auditLog;
            _isNew = false;
        }

        return auditLog;
    }

    /// <inheritdoc />
    public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        _pendingAuditLog = auditLog;
        _isNew = true;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_pendingAuditLog is null)
        {
            return;
        }

        EnsureConnectionOpen();

        var sql = _isNew
            ? databaseProvider.InsertSql
            : databaseProvider.UpdateSql;

        var command = new CommandDefinition(
            sql,
            new
            {
                _pendingAuditLog.LogId,
                _pendingAuditLog.SourceName,
                _pendingAuditLog.SourceKey,
                _pendingAuditLog.AuditLogXml
            },
            cancellationToken: cancellationToken);

        await dbConnection.ExecuteAsync(command);

        // Reset state
        _pendingAuditLog = null;
        _isNew = false;
    }

    private void EnsureConnectionOpen()
    {
        if (dbConnection.State != ConnectionState.Open)
        {
            dbConnection.Open();
        }
    }
}
