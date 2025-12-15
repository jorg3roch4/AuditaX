using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;

namespace AuditaX.Dapper.Services;

/// <summary>
/// Dapper implementation of the audit query service.
/// </summary>
/// <param name="connection">The database connection.</param>
/// <param name="provider">The database provider for SQL queries.</param>
public sealed class DapperAuditQueryService(
    IDbConnection connection,
    IDatabaseProvider provider) : IAuditQueryService
{
    private readonly IDbConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    private readonly IDatabaseProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    /// <inheritdoc />
    public async Task<IEnumerable<AuditQueryResult>> GetBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            throw new ArgumentException("SourceName cannot be null or empty.", nameof(sourceName));

        EnsureConnectionOpen();

        var results = await _connection.QueryAsync<AuditQueryResult>(
            _provider.GetSelectBySourceNameSql(skip, take),
            new { SourceName = sourceName, Skip = skip, Take = take });

        return results;
    }

    /// <inheritdoc />
    public async Task<AuditQueryResult?> GetBySourceNameAndKeyAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            throw new ArgumentException("SourceName cannot be null or empty.", nameof(sourceName));
        if (string.IsNullOrWhiteSpace(sourceKey))
            throw new ArgumentException("SourceKey cannot be null or empty.", nameof(sourceKey));

        EnsureConnectionOpen();

        var result = await _connection.QuerySingleOrDefaultAsync<AuditQueryResult>(
            _provider.SelectByEntitySql.Replace("AuditLogXml", "AuditLog"),
            new { SourceName = sourceName, SourceKey = sourceKey });

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditQueryResult>> GetBySourceNameAndDateAsync(
        string sourceName,
        DateTime fromDate,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            throw new ArgumentException("SourceName cannot be null or empty.", nameof(sourceName));

        EnsureConnectionOpen();

        var effectiveToDate = toDate ?? DateTime.UtcNow;

        var results = await _connection.QueryAsync<AuditQueryResult>(
            _provider.GetSelectBySourceNameAndDateSql(skip, take),
            new
            {
                SourceName = sourceName,
                FromDate = fromDate,
                ToDate = effectiveToDate,
                Skip = skip,
                Take = take
            });

        return results;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditQueryResult>> GetBySourceNameAndActionAsync(
        string sourceName,
        AuditAction action,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            throw new ArgumentException("SourceName cannot be null or empty.", nameof(sourceName));

        EnsureConnectionOpen();

        var results = await _connection.QueryAsync<AuditQueryResult>(
            _provider.SelectBySourceNameAndActionSql,
            new
            {
                SourceName = sourceName,
                Action = action.ToString()
            });

        return results;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditQueryResult>> GetBySourceNameActionAndDateAsync(
        string sourceName,
        AuditAction action,
        DateTime fromDate,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            throw new ArgumentException("SourceName cannot be null or empty.", nameof(sourceName));

        EnsureConnectionOpen();

        var effectiveToDate = toDate ?? DateTime.UtcNow;

        var results = await _connection.QueryAsync<AuditQueryResult>(
            _provider.SelectBySourceNameActionAndDateSql,
            new
            {
                SourceName = sourceName,
                Action = action.ToString(),
                FromDate = fromDate,
                ToDate = effectiveToDate
            });

        return results;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditSummaryResult>> GetSummaryBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            throw new ArgumentException("SourceName cannot be null or empty.", nameof(sourceName));

        EnsureConnectionOpen();

        var results = await _connection.QueryAsync<AuditSummaryResult>(
            _provider.GetSelectSummaryBySourceNameSql(skip, take),
            new { SourceName = sourceName, Skip = skip, Take = take });

        return results;
    }

    private void EnsureConnectionOpen()
    {
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }
    }
}
