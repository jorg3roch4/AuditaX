namespace AuditaX.Models;

/// <summary>
/// Represents a query result from the audit log table.
/// </summary>
public sealed record AuditQueryResult
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
    /// Gets or initializes the audit log content (XML or JSON depending on configuration).
    /// </summary>
    public string AuditLog { get; init; } = string.Empty;
}
