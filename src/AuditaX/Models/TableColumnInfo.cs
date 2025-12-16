namespace AuditaX.Models;

/// <summary>
/// Represents information about a database column.
/// Used for validating table structure at startup.
/// </summary>
public sealed class TableColumnInfo
{
    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type of the column.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum character length (for string types).
    /// Null for non-string types or unlimited length.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets whether the column allows null values.
    /// </summary>
    public bool IsNullable { get; set; }
}
