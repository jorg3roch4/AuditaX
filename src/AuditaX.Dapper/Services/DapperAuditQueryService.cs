using AuditaX.Enums;
using AuditaX.Exceptions;
using AuditaX.Interfaces;
using AuditaX.Models;

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
    public async Task<IEnumerable<AuditQueryResult>> GetBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        await ValidateSourceNameAsync(sourceName, cancellationToken);

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
        await ValidateSourceNameAndKeyAsync(sourceName, sourceKey, cancellationToken);

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
        await ValidateSourceNameAsync(sourceName, cancellationToken);

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

        return results;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditQueryResult>> GetBySourceNameAndActionAsync(
        string sourceName,
        AuditAction action,
        CancellationToken cancellationToken = default)
    {
        await ValidateSourceNameAsync(sourceName, cancellationToken);

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
        await ValidateSourceNameAsync(sourceName, cancellationToken);

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

        var results = await _connection.QueryAsync<AuditSummaryResult>(
            _provider.GetSelectSummaryBySourceNameSql(skip, take),
            new { SourceName = sourceName, Skip = skip, Take = take });

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

        var items = await _connection.QueryAsync<AuditQueryResult>(
            _provider.GetSelectBySourceNameSql(skip, take),
            new { SourceName = sourceName, Skip = skip, Take = take });

        var totalCount = await _connection.ExecuteScalarAsync<int>(
            _provider.CountBySourceNameSql,
            new { SourceName = sourceName });

        return new PagedResult<AuditQueryResult> { Items = items, TotalCount = totalCount };
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

        var effectiveToDate = toDate ?? DateTime.MaxValue;

        var items = await _connection.QueryAsync<AuditQueryResult>(
            _provider.GetSelectBySourceNameAndDateSql(skip, take),
            new { SourceName = sourceName, FromDate = fromDate, ToDate = effectiveToDate, Skip = skip, Take = take });

        var totalCount = await _connection.ExecuteScalarAsync<int>(
            _provider.CountBySourceNameAndDateSql,
            new { SourceName = sourceName, FromDate = fromDate, ToDate = effectiveToDate });

        return new PagedResult<AuditQueryResult> { Items = items, TotalCount = totalCount };
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

        var items = await _connection.QueryAsync<AuditQueryResult>(
            _provider.GetSelectBySourceNameAndActionSql(skip, take),
            new { SourceName = sourceName, Action = action.ToString(), Skip = skip, Take = take });

        var totalCount = await _connection.ExecuteScalarAsync<int>(
            _provider.CountBySourceNameAndActionSql,
            new { SourceName = sourceName, Action = action.ToString() });

        return new PagedResult<AuditQueryResult> { Items = items, TotalCount = totalCount };
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

        return new PagedResult<AuditQueryResult> { Items = items, TotalCount = totalCount };
    }

    /// <inheritdoc />
    public async Task<PagedResult<AuditSummaryResult>> GetPagedSummaryBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        await ValidateSourceNameAsync(sourceName, cancellationToken);

        var items = await _connection.QueryAsync<AuditSummaryResult>(
            _provider.GetSelectSummaryBySourceNameSql(skip, take),
            new { SourceName = sourceName, Skip = skip, Take = take });

        var totalCount = await _connection.ExecuteScalarAsync<int>(
            _provider.CountSummaryBySourceNameSql,
            new { SourceName = sourceName });

        return new PagedResult<AuditSummaryResult> { Items = items, TotalCount = totalCount };
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

        return new PagedResult<AuditSummaryResult> { Items = items, TotalCount = totalCount };
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

        EnsureConnectionOpen();

        var exists = await _connection.ExecuteScalarAsync<int>(
            _provider.SourceNameExistsSql,
            new { SourceName = sourceName });

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
        EnsureConnectionOpen();

        var sourceNameExists = await _connection.ExecuteScalarAsync<int>(
            _provider.SourceNameExistsSql,
            new { SourceName = sourceName });

        if (sourceNameExists == 0)
            throw new AuditSourceNotFoundException(sourceName);

        var sourceKeyExists = await _connection.ExecuteScalarAsync<int>(
            _provider.SourceKeyExistsSql,
            new { SourceName = sourceName, SourceKey = sourceKey });

        if (sourceKeyExists == 0)
            throw new AuditSourceKeyNotFoundException(sourceName, sourceKey);
    }

    private void EnsureConnectionOpen()
    {
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }
    }
}
