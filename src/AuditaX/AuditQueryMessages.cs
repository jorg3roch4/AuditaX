namespace AuditaX;

/// <summary>
/// Standardized error messages for audit query operations.
/// </summary>
public static class AuditQueryMessages
{
    /// <summary>Returned when 'SourceName' is null or whitespace.</summary>
    public const string SourceNameRequired = "'SourceName' is required.";

    /// <summary>Returned when 'SourceKey' is null or whitespace.</summary>
    public const string SourceKeyRequired = "'SourceKey' is required.";

    /// <summary>Returned when 'SourceName' exceeds the maximum allowed length.</summary>
    public static string SourceNameTooLong(int maxLength) =>
        $"'SourceName' cannot exceed {maxLength} characters.";

    /// <summary>Returned when 'SourceKey' exceeds the maximum allowed length.</summary>
    public static string SourceKeyTooLong(int maxLength) =>
        $"'SourceKey' cannot exceed {maxLength} characters.";

    /// <summary>Returned when no audit records exist for the given source name.</summary>
    public static string SourceNotFound(string sourceName) =>
        $"Source '{sourceName}' was not found in the audit log.";

    /// <summary>Returned when no audit record exists for the given source name and key combination.</summary>
    public static string SourceKeyNotFound(string sourceName, string sourceKey) =>
        $"Source '{sourceName}' with key '{sourceKey}' was not found in the audit log.";
}
