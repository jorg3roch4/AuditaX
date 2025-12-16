using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuditaX.Exceptions;

/// <summary>
/// Exception thrown when the audit log table exists but has an incorrect structure
/// (missing columns, wrong column types, etc.).
/// </summary>
public class AuditTableStructureMismatchException : Exception
{
    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Gets the list of missing columns.
    /// </summary>
    public IReadOnlyList<string> MissingColumns { get; }

    /// <summary>
    /// Gets the list of columns with incorrect types.
    /// Each entry is a tuple of (ColumnName, ExpectedType, ActualType).
    /// </summary>
    public IReadOnlyList<(string ColumnName, string ExpectedType, string ActualType)> IncorrectColumns { get; }

    /// <summary>
    /// Gets the SQL statement to create the correct audit table.
    /// </summary>
    public string CreateTableSql { get; }

    /// <summary>
    /// Initializes a new instance of the AuditTableStructureMismatchException.
    /// </summary>
    /// <param name="tableName">The name of the audit table.</param>
    /// <param name="missingColumns">List of missing column names.</param>
    /// <param name="incorrectColumns">List of columns with incorrect types.</param>
    /// <param name="createTableSql">The SQL statement to create the correct table.</param>
    public AuditTableStructureMismatchException(
        string tableName,
        IEnumerable<string> missingColumns,
        IEnumerable<(string ColumnName, string ExpectedType, string ActualType)> incorrectColumns,
        string createTableSql)
        : base(BuildMessage(tableName, missingColumns, incorrectColumns))
    {
        TableName = tableName;
        MissingColumns = missingColumns.ToList().AsReadOnly();
        IncorrectColumns = incorrectColumns.ToList().AsReadOnly();
        CreateTableSql = createTableSql;
    }

    private static string BuildMessage(
        string tableName,
        IEnumerable<string> missingColumns,
        IEnumerable<(string ColumnName, string ExpectedType, string ActualType)> incorrectColumns)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Audit table '{tableName}' exists but has an incorrect structure.");
        sb.AppendLine();

        var missingList = missingColumns.ToList();
        if (missingList.Count > 0)
        {
            sb.AppendLine("Missing columns:");
            foreach (var column in missingList)
            {
                sb.AppendLine($"  - {column}");
            }
            sb.AppendLine();
        }

        var incorrectList = incorrectColumns.ToList();
        if (incorrectList.Count > 0)
        {
            sb.AppendLine("Columns with incorrect types:");
            foreach (var (columnName, expectedType, actualType) in incorrectList)
            {
                sb.AppendLine($"  - {columnName}: expected '{expectedType}', found '{actualType}'");
            }
            sb.AppendLine();
        }

        sb.AppendLine("To fix this issue:");
        sb.AppendLine("  1. Drop the existing table and recreate it using the correct script, or");
        sb.AppendLine("  2. Set AutoCreateTable = true (this will NOT fix an existing table), or");
        sb.AppendLine("  3. Manually alter the table to match the expected structure.");

        return sb.ToString();
    }
}
