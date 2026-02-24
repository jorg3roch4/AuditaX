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
    /// Gets the SQL query to check if any audit log exists for the given SourceName.
    /// Returns 1 if at least one record exists, 0 otherwise.
    /// Uses parameter: @SourceName
    /// </summary>
    string SourceNameExistsSql { get; }

    /// <summary>
    /// Gets the SQL query to check if an audit log exists for the given SourceName and SourceKey.
    /// Returns 1 if a record exists, 0 otherwise.
    /// Uses parameters: @SourceName, @SourceKey
    /// </summary>
    string SourceKeyExistsSql { get; }

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

    /// <summary>
    /// Gets the SQL to count records by SourceName.
    /// Uses parameters: @SourceName
    /// </summary>
    string CountBySourceNameSql { get; }

    /// <summary>
    /// Gets the SQL to count records by SourceName filtered by date range.
    /// Uses parameters: @SourceName, @FromDate, @ToDate
    /// </summary>
    string CountBySourceNameAndDateSql { get; }

    /// <summary>
    /// Gets the SQL to count records by SourceName filtered by action.
    /// Uses parameters: @SourceName, @Action
    /// </summary>
    string CountBySourceNameAndActionSql { get; }

    /// <summary>
    /// Gets the SQL to count records by SourceName filtered by action and date range.
    /// Uses parameters: @SourceName, @Action, @FromDate, @ToDate
    /// </summary>
    string CountBySourceNameActionAndDateSql { get; }

    /// <summary>
    /// Gets the SQL to count summary records (distinct entities) by SourceName.
    /// Uses parameters: @SourceName
    /// </summary>
    string CountSummaryBySourceNameSql { get; }

    /// <summary>
    /// Gets the SQL query to select audit logs by SourceName filtered by action with pagination.
    /// Uses parameters: @SourceName, @Action, @Skip, @Take
    /// </summary>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <returns>SQL query string with pagination.</returns>
    string GetSelectBySourceNameAndActionSql(int skip, int take);

    /// <summary>
    /// Gets the SQL query to select audit logs by SourceName filtered by action and date range with pagination.
    /// Uses parameters: @SourceName, @Action, @FromDate, @ToDate, @Skip, @Take
    /// </summary>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <returns>SQL query string with pagination.</returns>
    string GetSelectBySourceNameActionAndDateSql(int skip, int take);

    /// <summary>
    /// Gets the SQL query to select a filtered summary by SourceName with optional sourceKey and date range filters.
    /// Returns: SourceName, SourceKey, LastAction, LastTimestamp, LastUser
    /// Uses parameters: @SourceName, @Skip, @Take, and optionally @SourceKey, @FromDate, @ToDate
    /// </summary>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="sourceKey">Optional source key filter. When not null, filters by exact SourceKey.</param>
    /// <param name="hasDateFilter">When true, adds date range filtering using @FromDate and @ToDate parameters.</param>
    /// <returns>SQL query string with pagination and optional filters.</returns>
    string GetSelectFilteredSummaryBySourceNameSql(int skip, int take, string? sourceKey, bool hasDateFilter);

    /// <summary>
    /// Gets the SQL to count filtered summary records by SourceName with optional sourceKey and date range.
    /// Uses parameters: @SourceName, and optionally @SourceKey, @FromDate, @ToDate
    /// </summary>
    /// <param name="sourceKey">Optional source key filter.</param>
    /// <param name="hasDateFilter">When true, adds date range filtering.</param>
    /// <returns>SQL count query string.</returns>
    string GetCountFilteredSummaryBySourceNameSql(string? sourceKey, bool hasDateFilter);

    #endregion
}
