namespace AuditaX.Exceptions;

/// <summary>
/// Exception thrown when a SourceKey for a given SourceName does not exist in the audit log table.
/// This indicates that no audit record exists for the specified entity instance.
/// </summary>
public sealed class AuditSourceKeyNotFoundException : Exception
{
    /// <summary>
    /// Gets the source name associated with the missing key.
    /// </summary>
    public string SourceName { get; }

    /// <summary>
    /// Gets the source key that was not found.
    /// </summary>
    public string SourceKey { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="AuditSourceKeyNotFoundException"/>.
    /// </summary>
    /// <param name="sourceName">The source name.</param>
    /// <param name="sourceKey">The source key that was not found.</param>
    public AuditSourceKeyNotFoundException(string sourceName, string sourceKey)
        : base($"Source '{sourceName}' with key '{sourceKey}' was not found in the audit log.")
    {
        SourceName = sourceName;
        SourceKey = sourceKey;
    }
}
