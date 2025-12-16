using System.Collections.Generic;
using AuditaX.Enums;
using AuditaX.Models;

namespace AuditaX.Interfaces;

/// <summary>
/// Provides database-specific SQL queries and configuration.
/// Implement this interface to add support for additional databases.
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>
    /// Gets the fully qualified table name including schema.
    /// </summary>
    string FullTableName { get; }

    /// <summary>
    /// Gets the name of the LogId column.
    /// </summary>
    string LogIdColumn { get; }

    /// <summary>
    /// Gets the name of the SourceName column.
    /// </summary>
    string SourceNameColumn { get; }

    /// <summary>
    /// Gets the name of the SourceKey column.
    /// </summary>
    string SourceKeyColumn { get; }

    /// <summary>
    /// Gets the name of the AuditLog column.
    /// </summary>
    string AuditLogColumn { get; }

    /// <summary>
    /// Gets the SQL query to select an audit log by entity.
    /// </summary>
    string SelectByEntitySql { get; }

    /// <summary>
    /// Gets the SQL query to insert a new audit log.
    /// </summary>
    string InsertSql { get; }

    /// <summary>
    /// Gets the SQL query to update an existing audit log.
    /// </summary>
    string UpdateSql { get; }

    /// <summary>
    /// Gets the SQL query to check if the audit table exists.
    /// </summary>
    string CheckTableExistsSql { get; }

    /// <summary>
    /// Gets the SQL query to create the audit table.
    /// </summary>
    string CreateTableSql { get; }

    /// <summary>
    /// Gets the SQL query to get the data type of the AuditLog column.
    /// Returns the column data type as a string (e.g., "xml", "nvarchar", "text").
    /// </summary>
    string GetAuditLogColumnTypeSql { get; }

    /// <summary>
    /// Gets the expected column type for XML format storage.
    /// </summary>
    string ExpectedXmlColumnType { get; }

    /// <summary>
    /// Gets the expected column type for JSON format storage.
    /// </summary>
    string ExpectedJsonColumnType { get; }

    /// <summary>
    /// Determines if the given column type is compatible with XML format.
    /// </summary>
    /// <param name="columnType">The actual column type from the database.</param>
    /// <returns>True if compatible with XML format; otherwise, false.</returns>
    bool IsXmlCompatibleColumnType(string columnType);

    /// <summary>
    /// Determines if the given column type is compatible with JSON format.
    /// </summary>
    /// <param name="columnType">The actual column type from the database.</param>
    /// <returns>True if compatible with JSON format; otherwise, false.</returns>
    bool IsJsonCompatibleColumnType(string columnType);

    /// <summary>
    /// Gets the SQL query to retrieve all columns of the audit table.
    /// Returns column_name, data_type, character_maximum_length, is_nullable.
    /// </summary>
    string GetTableStructureSql { get; }

    /// <summary>
    /// Gets the expected table structure based on the current configuration.
    /// </summary>
    /// <returns>A list of expected column definitions.</returns>
    IReadOnlyList<ExpectedColumnDefinition> GetExpectedTableStructure();

    /// <summary>
    /// Validates if the actual column matches the expected definition.
    /// </summary>
    /// <param name="actual">The actual column info from the database.</param>
    /// <param name="expected">The expected column definition.</param>
    /// <returns>True if the column is valid; false otherwise.</returns>
    bool ValidateColumn(TableColumnInfo actual, ExpectedColumnDefinition expected);

    #region Query SQL Properties

    /// <summary>
    /// Gets the SQL to create an index on SourceName for query optimization.
    /// </summary>
    string CreateSourceNameIndexSql { get; }

    /// <summary>
    /// Gets the SQL query to select audit logs by SourceName with pagination.
    /// Uses parameters: @SourceName, @Skip, @Take
    /// </summary>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <returns>SQL query string with pagination.</returns>
    string GetSelectBySourceNameSql(int skip, int take);

    /// <summary>
    /// Gets the SQL query to select audit logs by SourceName filtered by date range with pagination.
    /// Uses parameters: @SourceName, @FromDate, @ToDate, @Skip, @Take
    /// </summary>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <returns>SQL query string with pagination.</returns>
    string GetSelectBySourceNameAndDateSql(int skip, int take);

    /// <summary>
    /// Gets the SQL query to select audit logs by SourceName filtered by action.
    /// Uses parameters: @SourceName, @Action
    /// </summary>
    string SelectBySourceNameAndActionSql { get; }

    /// <summary>
    /// Gets the SQL query to select audit logs by SourceName filtered by action and date range.
    /// Uses parameters: @SourceName, @Action, @FromDate, @ToDate
    /// </summary>
    string SelectBySourceNameActionAndDateSql { get; }

    /// <summary>
    /// Gets the SQL query to select a summary (last event) for each entity by SourceName with pagination.
    /// Returns: SourceName, SourceKey, LastAction, LastTimestamp, LastUser
    /// Uses parameters: @SourceName, @Skip, @Take
    /// </summary>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <returns>SQL query string with pagination.</returns>
    string GetSelectSummaryBySourceNameSql(int skip, int take);

    #endregion
}
