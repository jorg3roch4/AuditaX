using AuditaX.Enums;
using AuditaX.Models;

namespace AuditaX.Interfaces;

/// <summary>
/// Provides audit logging functionality for tracking entity changes.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs the creation of an entity.
    /// </summary>
    /// <param name="sourceName">The name of the entity being audited.</param>
    /// <param name="sourceKey">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogCreateAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs the creation of an entity with a specific user.
    /// </summary>
    /// <param name="sourceName">The name of the entity being audited.</param>
    /// <param name="sourceKey">The unique identifier of the entity.</param>
    /// <param name="user">The user who created the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogCreateAsync(
        string sourceName,
        string sourceKey,
        string user,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an update event with the specified field changes.
    /// </summary>
    /// <param name="sourceName">The name of the entity being audited.</param>
    /// <param name="sourceKey">The unique identifier of the entity.</param>
    /// <param name="changes">The list of field changes to log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogUpdateAsync(
        string sourceName,
        string sourceKey,
        List<FieldChange> changes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an update event with the specified field changes and user.
    /// </summary>
    /// <param name="sourceName">The name of the entity being audited.</param>
    /// <param name="sourceKey">The unique identifier of the entity.</param>
    /// <param name="changes">The list of field changes to log.</param>
    /// <param name="user">The user who made the changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogUpdateAsync(
        string sourceName,
        string sourceKey,
        List<FieldChange> changes,
        string user,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs the deletion of an entity.
    /// </summary>
    /// <param name="sourceName">The name of the entity being audited.</param>
    /// <param name="sourceKey">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogDeleteAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs the deletion of an entity with a specific user.
    /// </summary>
    /// <param name="sourceName">The name of the entity being audited.</param>
    /// <param name="sourceKey">The unique identifier of the entity.</param>
    /// <param name="user">The user who deleted the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogDeleteAsync(
        string sourceName,
        string sourceKey,
        string user,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a related entity event (added or removed).
    /// </summary>
    /// <param name="sourceName">The name of the parent entity being audited.</param>
    /// <param name="sourceKey">The unique identifier of the parent entity.</param>
    /// <param name="action">The action (Added or Removed).</param>
    /// <param name="relatedName">The name of the related entity.</param>
    /// <param name="fields">The fields describing the related entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogRelatedAsync(
        string sourceName,
        string sourceKey,
        AuditAction action,
        string relatedName,
        List<FieldChange> fields,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a related entity event with a specific user.
    /// </summary>
    /// <param name="sourceName">The name of the parent entity being audited.</param>
    /// <param name="sourceKey">The unique identifier of the parent entity.</param>
    /// <param name="action">The action (Added or Removed).</param>
    /// <param name="relatedName">The name of the related entity.</param>
    /// <param name="fields">The fields describing the related entity.</param>
    /// <param name="user">The user who made the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogRelatedAsync(
        string sourceName,
        string sourceKey,
        AuditAction action,
        string relatedName,
        List<FieldChange> fields,
        string user,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the complete audit history for an entity.
    /// </summary>
    /// <param name="sourceName">The name of the entity.</param>
    /// <param name="sourceKey">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of audit log entries, or null if no history exists.</returns>
    Task<List<AuditLogEntry>?> GetAuditHistoryAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default);
}
