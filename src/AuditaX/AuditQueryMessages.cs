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

    /// <summary>Returned when an optional 'sourceKey' is provided but is empty or whitespace.</summary>
    public const string SourceKeyEmpty = "'sourceKey' cannot be empty or whitespace when provided.";

    /// <summary>Returned when 'skip' is negative.</summary>
    public const string SkipNegative = "'skip' must be greater than or equal to 0.";

    /// <summary>Returned when 'fromDate' is not earlier than or equal to 'toDate'.</summary>
    public const string DateRangeInverted = "'fromDate' must be earlier than or equal to 'toDate'.";

    /// <summary>Returned when 'SourceName' exceeds the maximum allowed length.</summary>
    public static string SourceNameTooLong(int maxLength) =>
        $"'SourceName' cannot exceed {maxLength} characters.";

    /// <summary>Returned when 'SourceKey' exceeds the maximum allowed length.</summary>
    public static string SourceKeyTooLong(int maxLength) =>
        $"'SourceKey' cannot exceed {maxLength} characters.";

    /// <summary>Returned when 'take' is outside the allowed range.</summary>
    public static string TakeOutOfRange(int maxTake) =>
        $"'take' must be between 1 and {maxTake}.";

    /// <summary>Returned when a date parameter does not have DateTimeKind.Utc.</summary>
    public static string DateKindInvalid(string paramName) =>
        $"'{paramName}' must be a UTC date (DateTimeKind.Utc).";

    /// <summary>Returned when an AuditAction enum value is not defined.</summary>
    public static string InvalidAction(int value) =>
        $"'action' value '{value}' is not a valid AuditAction.";

    /// <summary>Returned when no audit records exist for the given source name.</summary>
    public static string SourceNotFound(string sourceName) =>
        $"Source '{sourceName}' was not found in the audit log.";

    /// <summary>Returned when no audit record exists for the given source name and key combination.</summary>
    public static string SourceKeyNotFound(string sourceName, string sourceKey) =>
        $"Source '{sourceName}' with key '{sourceKey}' was not found in the audit log.";
}
