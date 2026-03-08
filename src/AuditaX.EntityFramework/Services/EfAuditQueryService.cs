using System.Data.Common;
using AuditaX;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;
using AuditaX.Validation;
using AuditaX.Wrappers;

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

    /// <inheritdoc />
    public async Task<Response<IEnumerable<AuditQueryResult>>> GetBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var error = AuditQueryValidator.ValidatePagination(skip, take);
        if (error is not null)
            return new Response<IEnumerable<AuditQueryResult>>(error);

        error = await ValidateSourceNameAsync(sourceName, cancellationToken);
        if (error is not null)
            return new Response<IEnumerable<AuditQueryResult>>(error);

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

        return new Response<IEnumerable<AuditQueryResult>>(results);
    }

    /// <inheritdoc />
    public async Task<Response<AuditQueryResult?>> GetBySourceNameAndKeyAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidateSourceNameAndKeyAsync(sourceName, sourceKey, cancellationToken);
        if (error is not null)
            return new Response<AuditQueryResult?>(error);

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = _provider.SelectByEntitySql.Replace("AuditLogXml", "AuditLog");
        AddParameter(command, "SourceName", sourceName);
        AddParameter(command, "SourceKey", sourceKey);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new Response<AuditQueryResult?>(MapToAuditQueryResult(reader));
        }

        return new Response<AuditQueryResult?>(data: null);
    }

    /// <inheritdoc />
    public async Task<Response<IEnumerable<AuditQueryResult>>> GetBySourceNameAndDateAsync(
        string sourceName,
        DateTime fromDate,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var error = AuditQueryValidator.ValidatePagination(skip, take);
        if (error is not null)
            return new Response<IEnumerable<AuditQueryResult>>(error);

        error = AuditQueryValidator.ValidateDateRange(fromDate, toDate);
        if (error is not null)
            return new Response<IEnumerable<AuditQueryResult>>(error);

        error = await ValidateSourceNameAsync(sourceName, cancellationToken);
        if (error is not null)
            return new Response<IEnumerable<AuditQueryResult>>(error);

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

        return new Response<IEnumerable<AuditQueryResult>>(results);
    }

    /// <inheritdoc />
    public async Task<Response<IEnumerable<AuditQueryResult>>> GetBySourceNameAndActionAsync(
        string sourceName,
        AuditAction action,
        CancellationToken cancellationToken = default)
    {
        var error = AuditQueryValidator.ValidateAction(action);
        if (error is not null)
            return new Response<IEnumerable<AuditQueryResult>>(error);

        error = await ValidateSourceNameAsync(sourceName, cancellationToken);
        if (error is not null)
            return new Response<IEnumerable<AuditQueryResult>>(error);

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

        return new Response<IEnumerable<AuditQueryResult>>(results);
    }

    /// <inheritdoc />
    public async Task<Response<IEnumerable<AuditQueryResult>>> GetBySourceNameActionAndDateAsync(
        string sourceName,
        AuditAction action,
        DateTime fromDate,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var error = AuditQueryValidator.ValidateAction(action);
        if (error is not null)
            return new Response<IEnumerable<AuditQueryResult>>(error);

        error = AuditQueryValidator.ValidateDateRange(fromDate, toDate);
        if (error is not null)
            return new Response<IEnumerable<AuditQueryResult>>(error);

        error = await ValidateSourceNameAsync(sourceName, cancellationToken);
        if (error is not null)
            return new Response<IEnumerable<AuditQueryResult>>(error);

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

        return new Response<IEnumerable<AuditQueryResult>>(results);
    }

    /// <inheritdoc />
    public async Task<Response<IEnumerable<AuditSummaryResult>>> GetSummaryBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var error = AuditQueryValidator.ValidatePagination(skip, take);
        if (error is not null)
            return new Response<IEnumerable<AuditSummaryResult>>(error);

        error = await ValidateSourceNameAsync(sourceName, cancellationToken);
        if (error is not null)
            return new Response<IEnumerable<AuditSummaryResult>>(error);

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

        return new Response<IEnumerable<AuditSummaryResult>>(results);
    }

    /// <inheritdoc />
    public async Task<PagedResponse<IEnumerable<AuditQueryResult>>> GetPagedBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var error = AuditQueryValidator.ValidatePagination(skip, take);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditQueryResult>>(error);

        error = await ValidateSourceNameAsync(sourceName, cancellationToken);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditQueryResult>>(error);

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

        return new PagedResponse<IEnumerable<AuditQueryResult>>(results, ToPageNumber(skip, take), take, totalCount);
    }

    /// <inheritdoc />
    public async Task<PagedResponse<IEnumerable<AuditQueryResult>>> GetPagedBySourceNameAndDateAsync(
        string sourceName,
        DateTime fromDate,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var error = AuditQueryValidator.ValidatePagination(skip, take);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditQueryResult>>(error);

        error = AuditQueryValidator.ValidateDateRange(fromDate, toDate);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditQueryResult>>(error);

        error = await ValidateSourceNameAsync(sourceName, cancellationToken);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditQueryResult>>(error);

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

        return new PagedResponse<IEnumerable<AuditQueryResult>>(results, ToPageNumber(skip, take), take, totalCount);
    }

    /// <inheritdoc />
    public async Task<PagedResponse<IEnumerable<AuditQueryResult>>> GetPagedBySourceNameAndActionAsync(
        string sourceName,
        AuditAction action,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var error = AuditQueryValidator.ValidatePagination(skip, take);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditQueryResult>>(error);

        error = AuditQueryValidator.ValidateAction(action);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditQueryResult>>(error);

        error = await ValidateSourceNameAsync(sourceName, cancellationToken);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditQueryResult>>(error);

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

        return new PagedResponse<IEnumerable<AuditQueryResult>>(results, ToPageNumber(skip, take), take, totalCount);
    }

    /// <inheritdoc />
    public async Task<PagedResponse<IEnumerable<AuditQueryResult>>> GetPagedBySourceNameActionAndDateAsync(
        string sourceName,
        AuditAction action,
        DateTime fromDate,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var error = AuditQueryValidator.ValidatePagination(skip, take);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditQueryResult>>(error);

        error = AuditQueryValidator.ValidateAction(action);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditQueryResult>>(error);

        error = AuditQueryValidator.ValidateDateRange(fromDate, toDate);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditQueryResult>>(error);

        error = await ValidateSourceNameAsync(sourceName, cancellationToken);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditQueryResult>>(error);

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

        return new PagedResponse<IEnumerable<AuditQueryResult>>(results, ToPageNumber(skip, take), take, totalCount);
    }

    /// <inheritdoc />
    public async Task<PagedResponse<IEnumerable<AuditSummaryResult>>> GetPagedSummaryBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var error = AuditQueryValidator.ValidatePagination(skip, take);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditSummaryResult>>(error);

        error = await ValidateSourceNameAsync(sourceName, cancellationToken);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditSummaryResult>>(error);

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

        return new PagedResponse<IEnumerable<AuditSummaryResult>>(results, ToPageNumber(skip, take), take, totalCount);
    }

    /// <inheritdoc />
    public async Task<PagedResponse<IEnumerable<AuditSummaryResult>>> GetPagedSummaryBySourceNameAsync(
        string sourceName,
        string? sourceKey,
        DateTime? fromDate,
        DateTime? toDate,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var error = AuditQueryValidator.ValidatePagination(skip, take);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditSummaryResult>>(error);

        error = AuditQueryValidator.ValidateOptionalSourceKey(sourceKey);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditSummaryResult>>(error);

        if (fromDate.HasValue)
        {
            error = AuditQueryValidator.ValidateDateRange(fromDate.Value, toDate);
            if (error is not null)
                return new PagedResponse<IEnumerable<AuditSummaryResult>>(error);
        }

        error = await ValidateSourceNameAsync(sourceName, cancellationToken);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditSummaryResult>>(error);

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

        return new PagedResponse<IEnumerable<AuditSummaryResult>>(results, ToPageNumber(skip, take), take, totalCount);
    }

    /// <inheritdoc />
    public async Task<Response<AuditDetailResult?>> GetParsedDetailBySourceNameAndKeyAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        var raw = await GetBySourceNameAndKeyAsync(sourceName, sourceKey, cancellationToken);
        if (!raw.Succeeded)
            return new Response<AuditDetailResult?>(raw.Message!);

        if (raw.Data is null)
            return new Response<AuditDetailResult?>(data: null);

        var entries = _changeLogService.ParseAuditLog(raw.Data.AuditLog);

        var detail = new AuditDetailResult
        {
            SourceName = raw.Data.SourceName,
            SourceKey = raw.Data.SourceKey,
            Entries = entries
        };

        return new Response<AuditDetailResult?>(detail);
    }

    private async Task<string?> ValidateSourceNameAsync(string sourceName, CancellationToken cancellationToken)
    {
        var error = AuditQueryValidator.ValidateSourceName(sourceName);
        if (error is not null)
            return error;

        var exists = await ExecuteCountAsync(
            _provider.SourceNameExistsSql,
            cmd => AddParameter(cmd, "SourceName", sourceName),
            cancellationToken);

        return exists == 0 ? AuditQueryMessages.SourceNotFound(sourceName) : null;
    }

    private async Task<string?> ValidateSourceNameAndKeyAsync(string sourceName, string sourceKey, CancellationToken cancellationToken)
    {
        var error = AuditQueryValidator.ValidateSourceName(sourceName);
        if (error is not null)
            return error;

        error = AuditQueryValidator.ValidateSourceKey(sourceKey);
        if (error is not null)
            return error;

        var sourceNameExists = await ExecuteCountAsync(
            _provider.SourceNameExistsSql,
            cmd => AddParameter(cmd, "SourceName", sourceName),
            cancellationToken);

        if (sourceNameExists == 0)
            return AuditQueryMessages.SourceNotFound(sourceName);

        var sourceKeyExists = await ExecuteCountAsync(
            _provider.SourceKeyExistsSql,
            cmd =>
            {
                AddParameter(cmd, "SourceName", sourceName);
                AddParameter(cmd, "SourceKey", sourceKey);
            },
            cancellationToken);

        return sourceKeyExists == 0 ? AuditQueryMessages.SourceKeyNotFound(sourceName, sourceKey) : null;
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

    private static int ToPageNumber(int skip, int take) =>
        take > 0 ? (skip / take) + 1 : 1;
}
