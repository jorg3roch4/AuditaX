using System.Data.Common;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;

namespace AuditaX.EntityFramework.Services;

/// <summary>
/// Entity Framework implementation of the audit query service.
/// Uses raw SQL queries for optimal performance with XML/JSON operations.
/// </summary>
/// <param name="context">The DbContext.</param>
/// <param name="provider">The database provider for SQL queries.</param>
public sealed class EfAuditQueryService(
    DbContext context,
    IDatabaseProvider provider) : IAuditQueryService
{
    private readonly DbContext _context = context ?? throw new ArgumentNullException(nameof(context));
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

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        List<AuditQueryResult> results = [];

        await using var command = connection.CreateCommand();
        command.CommandText = _provider.GetSelectBySourceNameSql(skip, take);
        AddParameter(command, "SourceName", sourceName);
        AddParameter(command, "Skip", skip);
        AddParameter(command, "Take", take);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapToAuditQueryResult(reader));
        }

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

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = _provider.SelectByEntitySql.Replace("AuditLogXml", "AuditLog");
        AddParameter(command, "SourceName", sourceName);
        AddParameter(command, "SourceKey", sourceKey);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToAuditQueryResult(reader);
        }

        return null;
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

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        var effectiveToDate = toDate ?? DateTime.UtcNow;
        List<AuditQueryResult> results = [];

        await using var command = connection.CreateCommand();
        command.CommandText = _provider.GetSelectBySourceNameAndDateSql(skip, take);
        AddParameter(command, "SourceName", sourceName);
        AddParameter(command, "FromDate", fromDate);
        AddParameter(command, "ToDate", effectiveToDate);
        AddParameter(command, "Skip", skip);
        AddParameter(command, "Take", take);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapToAuditQueryResult(reader));
        }

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

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        List<AuditQueryResult> results = [];

        await using var command = connection.CreateCommand();
        command.CommandText = _provider.SelectBySourceNameAndActionSql;
        AddParameter(command, "SourceName", sourceName);
        AddParameter(command, "Action", action.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapToAuditQueryResult(reader));
        }

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

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        var effectiveToDate = toDate ?? DateTime.UtcNow;
        List<AuditQueryResult> results = [];

        await using var command = connection.CreateCommand();
        command.CommandText = _provider.SelectBySourceNameActionAndDateSql;
        AddParameter(command, "SourceName", sourceName);
        AddParameter(command, "Action", action.ToString());
        AddParameter(command, "FromDate", fromDate);
        AddParameter(command, "ToDate", effectiveToDate);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapToAuditQueryResult(reader));
        }

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

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        List<AuditSummaryResult> results = [];

        await using var command = connection.CreateCommand();
        command.CommandText = _provider.GetSelectSummaryBySourceNameSql(skip, take);
        AddParameter(command, "SourceName", sourceName);
        AddParameter(command, "Skip", skip);
        AddParameter(command, "Take", take);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapToAuditSummaryResult(reader));
        }

        return results;
    }

    private static async Task EnsureConnectionOpenAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static AuditQueryResult MapToAuditQueryResult(DbDataReader reader)
    {
        return new AuditQueryResult
        {
            SourceName = reader.GetString(reader.GetOrdinal("SourceName")),
            SourceKey = reader.GetString(reader.GetOrdinal("SourceKey")),
            AuditLog = reader.IsDBNull(reader.GetOrdinal("AuditLog"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("AuditLog"))
        };
    }

    private static AuditSummaryResult MapToAuditSummaryResult(DbDataReader reader)
    {
        return new AuditSummaryResult
        {
            SourceName = reader.GetString(reader.GetOrdinal("SourceName")),
            SourceKey = reader.GetString(reader.GetOrdinal("SourceKey")),
            LastAction = reader.IsDBNull(reader.GetOrdinal("LastAction"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("LastAction")),
            LastTimestamp = reader.IsDBNull(reader.GetOrdinal("LastTimestamp"))
                ? DateTime.MinValue
                : reader.GetDateTime(reader.GetOrdinal("LastTimestamp")),
            LastUser = reader.IsDBNull(reader.GetOrdinal("LastUser"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("LastUser"))
        };
    }
}
