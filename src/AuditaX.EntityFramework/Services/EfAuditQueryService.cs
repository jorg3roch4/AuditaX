using System.Data.Common;
using AuditaX.Enums;
using AuditaX.Exceptions;
using AuditaX.Interfaces;
using AuditaX.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AuditaX.EntityFramework.Services;

/// <summary>
/// Entity Framework implementation of the audit query service.
/// Uses raw SQL queries for optimal performance with XML/JSON operations.
/// </summary>
/// <param name="context">The DbContext.</param>
/// <param name="provider">The database provider for SQL queries.</param>
/// <param name="changeLogService">The change log service for parsing audit logs.</param>
public sealed class EfAuditQueryService(
    DbContext context,
    IDatabaseProvider provider,
    IChangeLogService changeLogService) : IAuditQueryService
{
    private readonly DbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly IDatabaseProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    private readonly IChangeLogService _changeLogService = changeLogService ?? throw new ArgumentNullException(nameof(changeLogService));

    private const int MaxSourceNameLength = 64;
    private const int MaxSourceKeyLength = 64;

    /// <inheritdoc />
    public async Task<IEnumerable<AuditQueryResult>> GetBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        await ValidateSourceNameAsync(sourceName, cancellationToken);

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
        await ValidateSourceNameAndKeyAsync(sourceName, sourceKey, cancellationToken);

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
        await ValidateSourceNameAsync(sourceName, cancellationToken);

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        var effectiveToDate = toDate ?? DateTime.MaxValue;
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
        await ValidateSourceNameAsync(sourceName, cancellationToken);

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
        await ValidateSourceNameAsync(sourceName, cancellationToken);

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        var effectiveToDate = toDate ?? DateTime.MaxValue;
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
        await ValidateSourceNameAsync(sourceName, cancellationToken);

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

    /// <inheritdoc />
    public async Task<PagedResult<AuditQueryResult>> GetPagedBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        await ValidateSourceNameAsync(sourceName, cancellationToken);

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

        var totalCount = await ExecuteCountAsync(
            _provider.CountBySourceNameSql,
            cmd => AddParameter(cmd, "SourceName", sourceName),
            cancellationToken);

        return new PagedResult<AuditQueryResult> { Items = results, TotalCount = totalCount };
    }

    /// <inheritdoc />
    public async Task<PagedResult<AuditQueryResult>> GetPagedBySourceNameAndDateAsync(
        string sourceName,
        DateTime fromDate,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        await ValidateSourceNameAsync(sourceName, cancellationToken);

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        var effectiveToDate = toDate ?? DateTime.MaxValue;
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

        var totalCount = await ExecuteCountAsync(
            _provider.CountBySourceNameAndDateSql,
            cmd =>
            {
                AddParameter(cmd, "SourceName", sourceName);
                AddParameter(cmd, "FromDate", fromDate);
                AddParameter(cmd, "ToDate", effectiveToDate);
            },
            cancellationToken);

        return new PagedResult<AuditQueryResult> { Items = results, TotalCount = totalCount };
    }

    /// <inheritdoc />
    public async Task<PagedResult<AuditQueryResult>> GetPagedBySourceNameAndActionAsync(
        string sourceName,
        AuditAction action,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        await ValidateSourceNameAsync(sourceName, cancellationToken);

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        List<AuditQueryResult> results = [];

        await using var command = connection.CreateCommand();
        command.CommandText = _provider.GetSelectBySourceNameAndActionSql(skip, take);
        AddParameter(command, "SourceName", sourceName);
        AddParameter(command, "Action", action.ToString());
        AddParameter(command, "Skip", skip);
        AddParameter(command, "Take", take);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapToAuditQueryResult(reader));
        }

        var totalCount = await ExecuteCountAsync(
            _provider.CountBySourceNameAndActionSql,
            cmd =>
            {
                AddParameter(cmd, "SourceName", sourceName);
                AddParameter(cmd, "Action", action.ToString());
            },
            cancellationToken);

        return new PagedResult<AuditQueryResult> { Items = results, TotalCount = totalCount };
    }

    /// <inheritdoc />
    public async Task<PagedResult<AuditQueryResult>> GetPagedBySourceNameActionAndDateAsync(
        string sourceName,
        AuditAction action,
        DateTime fromDate,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        await ValidateSourceNameAsync(sourceName, cancellationToken);

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        var effectiveToDate = toDate ?? DateTime.MaxValue;
        List<AuditQueryResult> results = [];

        await using var command = connection.CreateCommand();
        command.CommandText = _provider.GetSelectBySourceNameActionAndDateSql(skip, take);
        AddParameter(command, "SourceName", sourceName);
        AddParameter(command, "Action", action.ToString());
        AddParameter(command, "FromDate", fromDate);
        AddParameter(command, "ToDate", effectiveToDate);
        AddParameter(command, "Skip", skip);
        AddParameter(command, "Take", take);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapToAuditQueryResult(reader));
        }

        var totalCount = await ExecuteCountAsync(
            _provider.CountBySourceNameActionAndDateSql,
            cmd =>
            {
                AddParameter(cmd, "SourceName", sourceName);
                AddParameter(cmd, "Action", action.ToString());
                AddParameter(cmd, "FromDate", fromDate);
                AddParameter(cmd, "ToDate", effectiveToDate);
            },
            cancellationToken);

        return new PagedResult<AuditQueryResult> { Items = results, TotalCount = totalCount };
    }

    /// <inheritdoc />
    public async Task<PagedResult<AuditSummaryResult>> GetPagedSummaryBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        await ValidateSourceNameAsync(sourceName, cancellationToken);

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

        var totalCount = await ExecuteCountAsync(
            _provider.CountSummaryBySourceNameSql,
            cmd => AddParameter(cmd, "SourceName", sourceName),
            cancellationToken);

        return new PagedResult<AuditSummaryResult> { Items = results, TotalCount = totalCount };
    }

    /// <inheritdoc />
    public async Task<PagedResult<AuditSummaryResult>> GetPagedSummaryBySourceNameAsync(
        string sourceName,
        string? sourceKey,
        DateTime? fromDate,
        DateTime? toDate,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        await ValidateSourceNameAsync(sourceName, cancellationToken);

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        var hasDateFilter = fromDate.HasValue;
        var effectiveToDate = toDate ?? DateTime.MaxValue;

        List<AuditSummaryResult> results = [];

        await using var command = connection.CreateCommand();
        command.CommandText = _provider.GetSelectFilteredSummaryBySourceNameSql(skip, take, sourceKey, hasDateFilter);
        AddParameter(command, "SourceName", sourceName);
        AddParameter(command, "Skip", skip);
        AddParameter(command, "Take", take);

        if (sourceKey is not null)
            AddParameter(command, "SourceKey", sourceKey);

        if (hasDateFilter)
        {
            AddParameter(command, "FromDate", fromDate!.Value);
            AddParameter(command, "ToDate", effectiveToDate);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapToAuditSummaryResult(reader));
        }

        var totalCount = await ExecuteCountAsync(
            _provider.GetCountFilteredSummaryBySourceNameSql(sourceKey, hasDateFilter),
            cmd =>
            {
                AddParameter(cmd, "SourceName", sourceName);
                if (sourceKey is not null)
                    AddParameter(cmd, "SourceKey", sourceKey);
                if (hasDateFilter)
                {
                    AddParameter(cmd, "FromDate", fromDate!.Value);
                    AddParameter(cmd, "ToDate", effectiveToDate);
                }
            },
            cancellationToken);

        return new PagedResult<AuditSummaryResult> { Items = results, TotalCount = totalCount };
    }

    /// <inheritdoc />
    public async Task<AuditDetailResult?> GetParsedDetailBySourceNameAndKeyAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        var raw = await GetBySourceNameAndKeyAsync(sourceName, sourceKey, cancellationToken);
        if (raw is null)
            return null;

        var entries = _changeLogService.ParseAuditLog(raw.AuditLog);

        return new AuditDetailResult
        {
            SourceName = raw.SourceName,
            SourceKey = raw.SourceKey,
            Entries = entries
        };
    }

    private async Task ValidateSourceNameAsync(string sourceName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            throw new ArgumentException("SourceName is required.", nameof(sourceName));

        if (sourceName.Length > MaxSourceNameLength)
            throw new ArgumentException($"SourceName cannot exceed {MaxSourceNameLength} characters.", nameof(sourceName));

        var exists = await ExecuteCountAsync(
            _provider.SourceNameExistsSql,
            cmd => AddParameter(cmd, "SourceName", sourceName),
            cancellationToken);

        if (exists == 0)
            throw new AuditSourceNotFoundException(sourceName);
    }

    private async Task ValidateSourceNameAndKeyAsync(string sourceName, string sourceKey, CancellationToken cancellationToken)
    {
        // Format checks first (no DB round-trip) to surface ArgumentException before AuditSourceNotFoundException
        if (string.IsNullOrWhiteSpace(sourceName))
            throw new ArgumentException("SourceName is required.", nameof(sourceName));

        if (sourceName.Length > MaxSourceNameLength)
            throw new ArgumentException($"SourceName cannot exceed {MaxSourceNameLength} characters.", nameof(sourceName));

        if (string.IsNullOrWhiteSpace(sourceKey))
            throw new ArgumentException("SourceKey is required.", nameof(sourceKey));

        if (sourceKey.Length > MaxSourceKeyLength)
            throw new ArgumentException($"SourceKey cannot exceed {MaxSourceKeyLength} characters.", nameof(sourceKey));

        // DB existence checks (ordered: sourceName first, then sourceKey)
        var sourceNameExists = await ExecuteCountAsync(
            _provider.SourceNameExistsSql,
            cmd => AddParameter(cmd, "SourceName", sourceName),
            cancellationToken);

        if (sourceNameExists == 0)
            throw new AuditSourceNotFoundException(sourceName);

        var sourceKeyExists = await ExecuteCountAsync(
            _provider.SourceKeyExistsSql,
            cmd =>
            {
                AddParameter(cmd, "SourceName", sourceName);
                AddParameter(cmd, "SourceKey", sourceKey);
            },
            cancellationToken);

        if (sourceKeyExists == 0)
            throw new AuditSourceKeyNotFoundException(sourceName, sourceKey);
    }

    private static async Task EnsureConnectionOpenAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }

    private async Task<int> ExecuteCountAsync(string sql, Action<DbCommand> addParams, CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        addParams(command);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
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
