namespace AuditaX.Exceptions;

/// <summary>
/// Exception thrown when a SourceName does not exist in the audit log table.
/// This indicates that no audit records have been created for the specified source type.
/// </summary>
public sealed class AuditSourceNotFoundException : Exception
{
    /// <summary>
    /// Gets the source name that was not found.
    /// </summary>
    public string SourceName { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="AuditSourceNotFoundException"/>.
    /// </summary>
    /// <param name="sourceName">The source name that was not found.</param>
    public AuditSourceNotFoundException(string sourceName)
        : base($"Source '{sourceName}' was not found in the audit log.")
    {
        SourceName = sourceName;
    }
}
