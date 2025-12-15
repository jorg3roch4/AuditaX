using System;
using System.Collections.Generic;
using AuditaX.Enums;

namespace AuditaX.Models;

/// <summary>
/// Represents a parsed audit log entry.
/// </summary>
public sealed record AuditLogEntry
{
    /// <summary>
    /// Gets or sets the action that was performed.
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// Gets or sets the user who performed the action.
    /// </summary>
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the action was performed (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the name of the related entity, if this is a related entity change.
    /// </summary>
    public string? Related { get; set; }

    /// <summary>
    /// Gets or sets the list of field changes in this entry.
    /// </summary>
    public List<FieldChange> Fields { get; set; } = [];
}
