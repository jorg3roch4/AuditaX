using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuditaX.Enums;
using AuditaX.Models;

namespace AuditaX.Interfaces;

/// <summary>
/// Service interface for querying audit logs.
/// </summary>
public interface IAuditQueryService
{
    /// <summary>
    /// Gets audit logs for a specific entity type with pagination.
    /// </summary>
    /// <param name="sourceName">The name of the entity type.</param>
    /// <param name="skip">Number of records to skip (default: 0).</param>
    /// <param name="take">Number of records to take (default: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of audit query results.</returns>
    Task<IEnumerable<AuditQueryResult>> GetBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the audit log for a specific entity instance.
    /// </summary>
    /// <param name="sourceName">The name of the entity type.</param>
    /// <param name="sourceKey">The unique key of the entity instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The audit query result if found, otherwise null.</returns>
    Task<AuditQueryResult?> GetBySourceNameAndKeyAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for entities that have events within a date range with pagination.
    /// Searches within the audit log content for matching timestamps.
    /// </summary>
    /// <param name="sourceName">The name of the entity type.</param>
    /// <param name="fromDate">The start date (UTC).</param>
    /// <param name="toDate">The end date (UTC). If null, searches up to current time.</param>
    /// <param name="skip">Number of records to skip (default: 0).</param>
    /// <param name="take">Number of records to take (default: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of audit query results.</returns>
    /// <remarks>
    /// This query searches within XML/JSON content which may be slow on large tables.
    /// Consider adding appropriate indexes or using summary queries for better performance.
    /// </remarks>
    Task<IEnumerable<AuditQueryResult>> GetBySourceNameAndDateAsync(
        string sourceName,
        DateTime fromDate,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for entities that have a specific action type.
    /// Searches within the audit log content for matching actions.
    /// </summary>
    /// <param name="sourceName">The name of the entity type.</param>
    /// <param name="action">The action type to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of audit query results.</returns>
    /// <remarks>
    /// WARNING: This query searches within XML/JSON content which may be slow on large tables.
    /// </remarks>
    Task<IEnumerable<AuditQueryResult>> GetBySourceNameAndActionAsync(
        string sourceName,
        AuditAction action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for entities that have a specific action within a date range.
    /// Searches within the audit log content for matching actions and timestamps.
    /// </summary>
    /// <param name="sourceName">The name of the entity type.</param>
    /// <param name="action">The action type to filter by.</param>
    /// <param name="fromDate">The start date (UTC).</param>
    /// <param name="toDate">The end date (UTC). If null, searches up to current time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of audit query results.</returns>
    /// <remarks>
    /// WARNING: This query searches within XML/JSON content which may be slow on large tables.
    /// </remarks>
    Task<IEnumerable<AuditQueryResult>> GetBySourceNameActionAndDateAsync(
        string sourceName,
        AuditAction action,
        DateTime fromDate,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a summary of audit logs showing the last event for each entity with pagination.
    /// This is an optimized query that extracts only the last entry information.
    /// </summary>
    /// <param name="sourceName">The name of the entity type.</param>
    /// <param name="skip">Number of records to skip (default: 0).</param>
    /// <param name="take">Number of records to take (default: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of audit summary results.</returns>
    /// <remarks>
    /// This query is more efficient than retrieving full audit logs when you only need
    /// to know the current state (last action) of each entity.
    /// </remarks>
    Task<IEnumerable<AuditSummaryResult>> GetSummaryBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by SourceName with pagination and total count.
    /// </summary>
    /// <param name="sourceName">The name of the entity type.</param>
    /// <param name="skip">Number of records to skip (default: 0).</param>
    /// <param name="take">Number of records to take (default: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result containing items and total count.</returns>
    Task<PagedResult<AuditQueryResult>> GetPagedBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by SourceName filtered by date range with pagination and total count.
    /// </summary>
    /// <param name="sourceName">The name of the entity type.</param>
    /// <param name="fromDate">The start date (UTC).</param>
    /// <param name="toDate">The end date (UTC). If null, searches up to current time.</param>
    /// <param name="skip">Number of records to skip (default: 0).</param>
    /// <param name="take">Number of records to take (default: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result containing items and total count.</returns>
    Task<PagedResult<AuditQueryResult>> GetPagedBySourceNameAndDateAsync(
        string sourceName,
        DateTime fromDate,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by SourceName filtered by action with pagination and total count.
    /// </summary>
    /// <param name="sourceName">The name of the entity type.</param>
    /// <param name="action">The action type to filter by.</param>
    /// <param name="skip">Number of records to skip (default: 0).</param>
    /// <param name="take">Number of records to take (default: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result containing items and total count.</returns>
    Task<PagedResult<AuditQueryResult>> GetPagedBySourceNameAndActionAsync(
        string sourceName,
        AuditAction action,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by SourceName filtered by action and date range with pagination and total count.
    /// </summary>
    /// <param name="sourceName">The name of the entity type.</param>
    /// <param name="action">The action type to filter by.</param>
    /// <param name="fromDate">The start date (UTC).</param>
    /// <param name="toDate">The end date (UTC). If null, searches up to current time.</param>
    /// <param name="skip">Number of records to skip (default: 0).</param>
    /// <param name="take">Number of records to take (default: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result containing items and total count.</returns>
    Task<PagedResult<AuditQueryResult>> GetPagedBySourceNameActionAndDateAsync(
        string sourceName,
        AuditAction action,
        DateTime fromDate,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged summary of audit logs showing the last event for each entity.
    /// </summary>
    /// <param name="sourceName">The name of the entity type.</param>
    /// <param name="skip">Number of records to skip (default: 0).</param>
    /// <param name="take">Number of records to take (default: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result containing summary items and total count.</returns>
    Task<PagedResult<AuditSummaryResult>> GetPagedSummaryBySourceNameAsync(
        string sourceName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a filtered and paged summary of audit logs with optional sourceKey and date range filters.
    /// </summary>
    /// <param name="sourceName">The name of the entity type.</param>
    /// <param name="sourceKey">Optional source key to filter by. When null, returns all entities.</param>
    /// <param name="fromDate">Optional start date (UTC). When null, no lower date bound.</param>
    /// <param name="toDate">Optional end date (UTC). When null, no upper date bound.</param>
    /// <param name="skip">Number of records to skip (default: 0).</param>
    /// <param name="take">Number of records to take (default: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result containing summary items and total count.</returns>
    Task<PagedResult<AuditSummaryResult>> GetPagedSummaryBySourceNameAsync(
        string sourceName,
        string? sourceKey,
        DateTime? fromDate,
        DateTime? toDate,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the parsed audit detail for a specific entity, with typed Before/After/Value fields.
    /// The returned entries have no raw XML/JSON; all data is already parsed into strongly-typed objects.
    /// </summary>
    /// <param name="sourceName">The name of the entity type.</param>
    /// <param name="sourceKey">The unique key of the entity instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed audit detail, or null if the entity was not found.</returns>
    Task<AuditDetailResult?> GetParsedDetailBySourceNameAndKeyAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default);
}
