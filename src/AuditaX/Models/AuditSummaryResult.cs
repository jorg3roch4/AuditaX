using System;

namespace AuditaX.Models;

/// <summary>
/// Represents a summary result showing the last audit event for an entity.
/// </summary>
public sealed record AuditSummaryResult
{
    /// <summary>
    /// Gets or initializes the name of the audited entity.
    /// </summary>
    public string SourceName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the unique key of the audited entity.
    /// </summary>
    public string SourceKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the last action performed (Created, Updated, Deleted, etc.).
    /// </summary>
    public string LastAction { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the timestamp of the last action (UTC).
    /// </summary>
    public DateTime LastTimestamp { get; init; }

    /// <summary>
    /// Gets or initializes the user who performed the last action.
    /// </summary>
    public string LastUser { get; init; } = string.Empty;
}
