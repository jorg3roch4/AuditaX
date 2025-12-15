using System.Threading;
using System.Threading.Tasks;
using AuditaX.Entities;

namespace AuditaX.Interfaces;

/// <summary>
/// Repository interface for audit log persistence operations.
/// </summary>
public interface IAuditRepository
{
    /// <summary>
    /// Gets an audit log by entity name and key without tracking.
    /// </summary>
    /// <param name="sourceName">The name of the entity.</param>
    /// <param name="sourceKey">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The audit log if found, otherwise null.</returns>
    Task<AuditLog?> GetByEntityAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an audit log by entity name and key with tracking for updates.
    /// </summary>
    /// <param name="sourceName">The name of the entity.</param>
    /// <param name="sourceKey">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The audit log if found, otherwise null.</returns>
    Task<AuditLog?> GetByEntityTrackingAsync(
        string sourceName,
        string sourceKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new audit log record.
    /// </summary>
    /// <param name="auditLog">The audit log to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
