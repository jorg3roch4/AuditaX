using System.Collections.Generic;

namespace AuditaX.Models;

/// <summary>
/// Represents a parsed audit detail for a specific entity instance.
/// Contains the fully parsed entries with typed fields (no raw XML/JSON).
/// </summary>
public sealed record AuditDetailResult
{
    /// <summary>
    /// Gets the name of the entity type.
    /// </summary>
    public string SourceName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the unique key of the entity instance.
    /// </summary>
    public string SourceKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets the list of parsed audit log entries with typed Before/After/Value fields.
    /// </summary>
    public List<AuditLogEntry> Entries { get; init; } = [];
}
