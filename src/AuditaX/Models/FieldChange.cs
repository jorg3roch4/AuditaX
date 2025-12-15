namespace AuditaX.Models;

/// <summary>
/// Represents a single field change in an audit log entry.
/// </summary>
public sealed record FieldChange
{
    /// <summary>
    /// Gets or sets the name of the field that changed.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value before the change.
    /// Used for Update actions.
    /// </summary>
    public string? Before { get; set; }

    /// <summary>
    /// Gets or sets the value after the change.
    /// Used for Update actions.
    /// </summary>
    public string? After { get; set; }

    /// <summary>
    /// Gets or sets the value of the field.
    /// Used for Added/Removed actions where before/after is not applicable.
    /// </summary>
    public string? Value { get; set; }
}
