namespace AuditaX.Enums;

/// <summary>
/// Represents the type of audit action performed on an entity.
/// </summary>
public enum AuditAction
{
    /// <summary>
    /// The entity was created.
    /// </summary>
    Created,

    /// <summary>
    /// The entity was updated.
    /// </summary>
    Updated,

    /// <summary>
    /// The entity was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// A related entity was added.
    /// </summary>
    Added,

    /// <summary>
    /// A related entity was removed.
    /// </summary>
    Removed
}
