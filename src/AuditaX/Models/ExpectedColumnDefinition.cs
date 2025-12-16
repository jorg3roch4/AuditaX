namespace AuditaX.Models;

/// <summary>
/// Represents the expected definition of a database column.
/// Used for validating table structure at startup.
/// </summary>
public sealed class ExpectedColumnDefinition
{
    /// <summary>
    /// Gets or sets the expected column name.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected data type(s).
    /// Multiple values can be provided for types that may vary (e.g., "nvarchar" or "varchar").
    /// </summary>
    public string[] AcceptableDataTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the minimum expected length for string columns.
    /// Null means no minimum length requirement.
    /// -1 means MAX/unlimited length is expected.
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Gets or sets whether the column must be non-nullable.
    /// </summary>
    public bool RequireNotNull { get; set; }

    /// <summary>
    /// Gets a human-readable description of the expected type.
    /// </summary>
    public string ExpectedTypeDescription
    {
        get
        {
            var types = string.Join(" or ", AcceptableDataTypes);
            if (MinLength.HasValue)
            {
                if (MinLength.Value == -1)
                    return $"{types}(MAX)";
                return $"{types}({MinLength.Value}+)";
            }
            return types;
        }
    }
}
