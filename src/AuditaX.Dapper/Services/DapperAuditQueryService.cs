using AuditaX;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;
using AuditaX.Wrappers;

namespace AuditaX.Dapper.Services;

/// <summary>
/// Dapper implementation of the audit query service.
/// </summary>
/// <param name="connection">The database connection.</param>
/// <param name="provider">The database provider for SQL queries.</param>
/// <param name="changeLogService">The change log service for parsing audit logs.</param>
public sealed class DapperAuditQueryService(
    IDbConnection connection,
    IDatabaseProvider provider,
    IChangeLogService changeLogService) : IAuditQueryService
{
    private readonly IDbConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    private readonly IDatabaseProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    private readonly IChangeLogService _changeLogService = changeLogService ?? throw new ArgumentNullException(nameof(changeLogService));

    private const int MaxSourceNameLength = 64;
    private const int MaxSourceKeyLength = 64;

    /// <inheritdoc />
    public async Task<Response<IEnumerable<AuditQueryResult>>> GetBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidateSourceNameAsync(sourceName);
        if (error is not null)
            return new Response<IEnumerable<AuditQueryResult>>(error);

        var results = await _connection.QueryAsync<AuditQueryResult>(
            _provider.GetSelectBySourceNameSql(skip, take),
            new { SourceName = sourceName, Skip = skip, Take = take });

        return new Response<IEnumerable<AuditQueryResult>>(results);
    }

    /// <inheritdoc />
    public async Task<Response<AuditQueryResult?>> GetBySourceNameAndKeyAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidateSourceNameAndKeyAsync(sourceName, sourceKey);
        if (error is not null)
            return new Response<AuditQueryResult?>(error);

        var result = await _connection.QuerySingleOrDefaultAsync<AuditQueryResult>(
            _provider.SelectByEntitySql.Replace("AuditLogXml", "AuditLog"),
            new { SourceName = sourceName, SourceKey = sourceKey });

        return new Response<AuditQueryResult?>(result);
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
        var error = await ValidateSourceNameAsync(sourceName);
        if (error is not null)
            return new Response<IEnumerable<AuditQueryResult>>(error);

        var effectiveToDate = toDate ?? DateTime.MaxValue;

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

        return new Response<IEnumerable<AuditQueryResult>>(results);
    }

    /// <inheritdoc />
    public async Task<Response<IEnumerable<AuditQueryResult>>> GetBySourceNameAndActionAsync(
        string sourceName,
        AuditAction action,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidateSourceNameAsync(sourceName);
        if (error is not null)
            return new Response<IEnumerable<AuditQueryResult>>(error);

        var results = await _connection.QueryAsync<AuditQueryResult>(
            _provider.SelectBySourceNameAndActionSql,
            new
            {
                SourceName = sourceName,
                Action = action.ToString()
            });

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
        var error = await ValidateSourceNameAsync(sourceName);
        if (error is not null)
            return new Response<IEnumerable<AuditQueryResult>>(error);

        var effectiveToDate = toDate ?? DateTime.MaxValue;

        var results = await _connection.QueryAsync<AuditQueryResult>(
            _provider.SelectBySourceNameActionAndDateSql,
            new
            {
                SourceName = sourceName,
                Action = action.ToString(),
                FromDate = fromDate,
                ToDate = effectiveToDate
            });

        return new Response<IEnumerable<AuditQueryResult>>(results);
    }

    /// <inheritdoc />
    public async Task<Response<IEnumerable<AuditSummaryResult>>> GetSummaryBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidateSourceNameAsync(sourceName);
        if (error is not null)
            return new Response<IEnumerable<AuditSummaryResult>>(error);

        var results = await _connection.QueryAsync<AuditSummaryResult>(
            _provider.GetSelectSummaryBySourceNameSql(skip, take),
            new { SourceName = sourceName, Skip = skip, Take = take });

        return new Response<IEnumerable<AuditSummaryResult>>(results);
    }

    /// <inheritdoc />
    public async Task<PagedResponse<IEnumerable<AuditQueryResult>>> GetPagedBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidateSourceNameAsync(sourceName);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditQueryResult>>(error);

        var items = await _connection.QueryAsync<AuditQueryResult>(
            _provider.GetSelectBySourceNameSql(skip, take),
            new { SourceName = sourceName, Skip = skip, Take = take });

        var totalCount = await _connection.ExecuteScalarAsync<int>(
            _provider.CountBySourceNameSql,
            new { SourceName = sourceName });

        return new PagedResponse<IEnumerable<AuditQueryResult>>(items, ToPageNumber(skip, take), take, totalCount);
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
        var error = await ValidateSourceNameAsync(sourceName);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditQueryResult>>(error);

        var effectiveToDate = toDate ?? DateTime.MaxValue;

        var items = await _connection.QueryAsync<AuditQueryResult>(
            _provider.GetSelectBySourceNameAndDateSql(skip, take),
            new { SourceName = sourceName, FromDate = fromDate, ToDate = effectiveToDate, Skip = skip, Take = take });

        var totalCount = await _connection.ExecuteScalarAsync<int>(
            _provider.CountBySourceNameAndDateSql,
            new { SourceName = sourceName, FromDate = fromDate, ToDate = effectiveToDate });

        return new PagedResponse<IEnumerable<AuditQueryResult>>(items, ToPageNumber(skip, take), take, totalCount);
    }

    /// <inheritdoc />
    public async Task<PagedResponse<IEnumerable<AuditQueryResult>>> GetPagedBySourceNameAndActionAsync(
        string sourceName,
        AuditAction action,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidateSourceNameAsync(sourceName);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditQueryResult>>(error);

        var items = await _connection.QueryAsync<AuditQueryResult>(
            _provider.GetSelectBySourceNameAndActionSql(skip, take),
            new { SourceName = sourceName, Action = action.ToString(), Skip = skip, Take = take });

        var totalCount = await _connection.ExecuteScalarAsync<int>(
            _provider.CountBySourceNameAndActionSql,
            new { SourceName = sourceName, Action = action.ToString() });

        return new PagedResponse<IEnumerable<AuditQueryResult>>(items, ToPageNumber(skip, take), take, totalCount);
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
        var error = await ValidateSourceNameAsync(sourceName);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditQueryResult>>(error);

        var effectiveToDate = toDate ?? DateTime.MaxValue;

        var items = await _connection.QueryAsync<AuditQueryResult>(
            _provider.GetSelectBySourceNameActionAndDateSql(skip, take),
            new
            {
                SourceName = sourceName,
                Action = action.ToString(),
                FromDate = fromDate,
                ToDate = effectiveToDate,
                Skip = skip,
                Take = take
            });

        var totalCount = await _connection.ExecuteScalarAsync<int>(
            _provider.CountBySourceNameActionAndDateSql,
            new
            {
                SourceName = sourceName,
                Action = action.ToString(),
                FromDate = fromDate,
                ToDate = effectiveToDate
            });

        return new PagedResponse<IEnumerable<AuditQueryResult>>(items, ToPageNumber(skip, take), take, totalCount);
    }

    /// <inheritdoc />
    public async Task<PagedResponse<IEnumerable<AuditSummaryResult>>> GetPagedSummaryBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidateSourceNameAsync(sourceName);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditSummaryResult>>(error);

        var items = await _connection.QueryAsync<AuditSummaryResult>(
            _provider.GetSelectSummaryBySourceNameSql(skip, take),
            new { SourceName = sourceName, Skip = skip, Take = take });

        var totalCount = await _connection.ExecuteScalarAsync<int>(
            _provider.CountSummaryBySourceNameSql,
            new { SourceName = sourceName });

        return new PagedResponse<IEnumerable<AuditSummaryResult>>(items, ToPageNumber(skip, take), take, totalCount);
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
        var error = await ValidateSourceNameAsync(sourceName);
        if (error is not null)
            return new PagedResponse<IEnumerable<AuditSummaryResult>>(error);

        var hasDateFilter = fromDate.HasValue;
        var effectiveToDate = toDate ?? DateTime.MaxValue;

        var parameters = new DynamicParameters();
        parameters.Add("SourceName", sourceName);
        parameters.Add("Skip", skip);
        parameters.Add("Take", take);

        if (sourceKey is not null)
            parameters.Add("SourceKey", sourceKey);

        if (hasDateFilter)
        {
            parameters.Add("FromDate", fromDate!.Value);
            parameters.Add("ToDate", effectiveToDate);
        }

        var items = await _connection.QueryAsync<AuditSummaryResult>(
            _provider.GetSelectFilteredSummaryBySourceNameSql(skip, take, sourceKey, hasDateFilter),
            parameters);

        var totalCount = await _connection.ExecuteScalarAsync<int>(
            _provider.GetCountFilteredSummaryBySourceNameSql(sourceKey, hasDateFilter),
            parameters);

        return new PagedResponse<IEnumerable<AuditSummaryResult>>(items, ToPageNumber(skip, take), take, totalCount);
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

        return new Response<AuditDetailResult?>(new AuditDetailResult
        {
            SourceName = raw.Data.SourceName,
            SourceKey = raw.Data.SourceKey,
            Entries = entries
        });
    }

    private async Task<string?> ValidateSourceNameAsync(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return AuditQueryMessages.SourceNameRequired;

        if (sourceName.Length > MaxSourceNameLength)
            return AuditQueryMessages.SourceNameTooLong(MaxSourceNameLength);

        EnsureConnectionOpen();

        var exists = await _connection.ExecuteScalarAsync<int>(
            _provider.SourceNameExistsSql,
            new { SourceName = sourceName });

        return exists == 0 ? AuditQueryMessages.SourceNotFound(sourceName) : null;
    }

    private async Task<string?> ValidateSourceNameAndKeyAsync(string sourceName, string sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return AuditQueryMessages.SourceNameRequired;

        if (sourceName.Length > MaxSourceNameLength)
            return AuditQueryMessages.SourceNameTooLong(MaxSourceNameLength);

        if (string.IsNullOrWhiteSpace(sourceKey))
            return AuditQueryMessages.SourceKeyRequired;

        if (sourceKey.Length > MaxSourceKeyLength)
            return AuditQueryMessages.SourceKeyTooLong(MaxSourceKeyLength);

        EnsureConnectionOpen();

        var sourceNameExists = await _connection.ExecuteScalarAsync<int>(
            _provider.SourceNameExistsSql,
            new { SourceName = sourceName });

        if (sourceNameExists == 0)
            return AuditQueryMessages.SourceNotFound(sourceName);

        var sourceKeyExists = await _connection.ExecuteScalarAsync<int>(
            _provider.SourceKeyExistsSql,
            new { SourceName = sourceName, SourceKey = sourceKey });

        return sourceKeyExists == 0 ? AuditQueryMessages.SourceKeyNotFound(sourceName, sourceKey) : null;
    }

    private void EnsureConnectionOpen()
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();
    }

    private static int ToPageNumber(int skip, int take) =>
        take > 0 ? (skip / take) + 1 : 1;
}
