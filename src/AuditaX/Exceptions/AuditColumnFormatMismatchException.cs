using System;
using AuditaX.Enums;

namespace AuditaX.Exceptions;

/// <summary>
/// Exception thrown when the audit log column type in the database does not match
/// the configured ChangeLogFormat (XML vs JSON).
/// </summary>
public class AuditColumnFormatMismatchException : Exception
{
    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Gets the column name.
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// Gets the expected format based on configuration.
    /// </summary>
    public ChangeLogFormat ExpectedFormat { get; }

    /// <summary>
    /// Gets the actual column type in the database.
    /// </summary>
    public string ActualColumnType { get; }

    /// <summary>
    /// Gets the expected column type based on configuration.
    /// </summary>
    public string ExpectedColumnType { get; }

    /// <summary>
    /// Initializes a new instance of the AuditColumnFormatMismatchException.
    /// </summary>
    /// <param name="tableName">The name of the audit table.</param>
    /// <param name="columnName">The name of the audit log column.</param>
    /// <param name="expectedFormat">The expected format based on configuration.</param>
    /// <param name="expectedColumnType">The expected column type.</param>
    /// <param name="actualColumnType">The actual column type in the database.</param>
    public AuditColumnFormatMismatchException(
        string tableName,
        string columnName,
        ChangeLogFormat expectedFormat,
        string expectedColumnType,
        string actualColumnType)
        : base(BuildMessage(tableName, columnName, expectedFormat, expectedColumnType, actualColumnType))
    {
        TableName = tableName;
        ColumnName = columnName;
        ExpectedFormat = expectedFormat;
        ExpectedColumnType = expectedColumnType;
        ActualColumnType = actualColumnType;
    }

    /// <summary>
    /// Initializes a new instance of the AuditColumnFormatMismatchException with an inner exception.
    /// </summary>
    /// <param name="tableName">The name of the audit table.</param>
    /// <param name="columnName">The name of the audit log column.</param>
    /// <param name="expectedFormat">The expected format based on configuration.</param>
    /// <param name="expectedColumnType">The expected column type.</param>
    /// <param name="actualColumnType">The actual column type in the database.</param>
    /// <param name="innerException">The inner exception.</param>
    public AuditColumnFormatMismatchException(
        string tableName,
        string columnName,
        ChangeLogFormat expectedFormat,
        string expectedColumnType,
        string actualColumnType,
        Exception innerException)
        : base(BuildMessage(tableName, columnName, expectedFormat, expectedColumnType, actualColumnType), innerException)
    {
        TableName = tableName;
        ColumnName = columnName;
        ExpectedFormat = expectedFormat;
        ExpectedColumnType = expectedColumnType;
        ActualColumnType = actualColumnType;
    }

    private static string BuildMessage(
        string tableName,
        string columnName,
        ChangeLogFormat expectedFormat,
        string expectedColumnType,
        string actualColumnType)
    {
        return $"Audit column format mismatch in table '{tableName}'. " +
               $"Configuration specifies ChangeLogFormat.{expectedFormat} which requires column '{columnName}' " +
               $"to be of type '{expectedColumnType}', but the actual column type is '{actualColumnType}'. " +
               $"Please either:\n" +
               $"  1. Change the configuration to match the database column type, or\n" +
               $"  2. Recreate the audit table with the correct column type using the appropriate script:\n" +
               $"     - For XML format: Use 01_CreateAuditTable_XML.sql\n" +
               $"     - For JSON format: Use 02_CreateAuditTable_JSON.sql";
    }
}
