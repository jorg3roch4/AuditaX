using AuditaX.Enums;

namespace AuditaX.Validation;

/// <summary>
/// Provides static validation methods for audit query input parameters.
/// All methods return null on success or an error message string on failure.
/// </summary>
public static class AuditQueryValidator
{
    /// <summary>Maximum allowed length for SourceName.</summary>
    public const int MaxSourceNameLength = 64;

    /// <summary>Maximum allowed length for SourceKey.</summary>
    public const int MaxSourceKeyLength = 64;

    /// <summary>Maximum allowed value for the 'take' pagination parameter.</summary>
    public const int MaxTake = 1000;

    /// <summary>
    /// Validates 'sourceName': must not be null/whitespace and must not exceed the max length.
    /// </summary>
    public static string? ValidateSourceName(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return AuditQueryMessages.SourceNameRequired;

        if (sourceName.Length > MaxSourceNameLength)
            return AuditQueryMessages.SourceNameTooLong(MaxSourceNameLength);

        return null;
    }

    /// <summary>
    /// Validates a required 'sourceKey': must not be null/whitespace and must not exceed the max length.
    /// </summary>
    public static string? ValidateSourceKey(string sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
            return AuditQueryMessages.SourceKeyRequired;

        if (sourceKey.Length > MaxSourceKeyLength)
            return AuditQueryMessages.SourceKeyTooLong(MaxSourceKeyLength);

        return null;
    }

    /// <summary>
    /// Validates an optional 'sourceKey'. When null the filter is skipped and no error is returned.
    /// When provided, must not be empty/whitespace and must not exceed the max length.
    /// </summary>
    public static string? ValidateOptionalSourceKey(string? sourceKey)
    {
        if (sourceKey is null)
            return null;

        if (string.IsNullOrWhiteSpace(sourceKey))
            return AuditQueryMessages.SourceKeyEmpty;

        if (sourceKey.Length > MaxSourceKeyLength)
            return AuditQueryMessages.SourceKeyTooLong(MaxSourceKeyLength);

        return null;
    }

    /// <summary>
    /// Validates pagination parameters: skip must be >= 0, take must be between 1 and <see cref="MaxTake"/>.
    /// </summary>
    public static string? ValidatePagination(int skip, int take)
    {
        if (skip < 0)
            return AuditQueryMessages.SkipNegative;

        if (take < 1 || take > MaxTake)
            return AuditQueryMessages.TakeOutOfRange(MaxTake);

        return null;
    }

    /// <summary>
    /// Validates date parameters: both must be <see cref="DateTimeKind.Utc"/>,
    /// and when <paramref name="toDate"/> is provided it must be >= <paramref name="fromDate"/>.
    /// </summary>
    public static string? ValidateDateRange(DateTime fromDate, DateTime? toDate)
    {
        if (fromDate.Kind != DateTimeKind.Utc)
            return AuditQueryMessages.DateKindInvalid("fromDate");

        if (toDate.HasValue)
        {
            if (toDate.Value.Kind != DateTimeKind.Utc)
                return AuditQueryMessages.DateKindInvalid("toDate");

            if (fromDate > toDate.Value)
                return AuditQueryMessages.DateRangeInverted;
        }

        return null;
    }

    /// <summary>
    /// Validates that the given <paramref name="action"/> is a defined <see cref="AuditAction"/> enum value.
    /// </summary>
    public static string? ValidateAction(AuditAction action)
    {
        if (!Enum.IsDefined(action))
            return AuditQueryMessages.InvalidAction((int)action);

        return null;
    }
}
