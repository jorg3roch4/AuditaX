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
}
