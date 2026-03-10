using System;
using System.Collections.Generic;
using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;

namespace AuditaX.SqlServer.Providers;

/// <summary>
/// SQL Server database provider implementation.
/// </summary>
/// <param name="options">The audit options.</param>
public sealed class SqlServerDatabaseProvider(AuditaXOptions options) : IDatabaseProvider
{

    /// <inheritdoc />
    public string FullTableName => $"[{options.Schema}].[{options.TableName}]";

    /// <inheritdoc />
    public string LogIdColumn => "LogId";

    /// <inheritdoc />
    public string SourceNameColumn => "SourceName";

    /// <inheritdoc />
    public string SourceKeyColumn => "SourceKey";

    /// <inheritdoc />
    public string AuditLogColumn => "AuditLog";

    /// <inheritdoc />
    public string SelectByEntitySql =>
        $@"SELECT [{LogIdColumn}], [{SourceNameColumn}], [{SourceKeyColumn}], [{AuditLogColumn}] AS AuditLogXml
           FROM {FullTableName}
           WHERE [{SourceNameColumn}] = @SourceName AND [{SourceKeyColumn}] = @SourceKey";

    /// <inheritdoc />
    public string InsertSql =>
        $@"INSERT INTO {FullTableName} ([{LogIdColumn}], [{SourceNameColumn}], [{SourceKeyColumn}], [{AuditLogColumn}])
           VALUES (@LogId, @SourceName, @SourceKey, @AuditLogXml)";

    /// <inheritdoc />
    public string UpdateSql =>
        $@"UPDATE {FullTableName}
           SET [{AuditLogColumn}] = @AuditLogXml
           WHERE [{SourceNameColumn}] = @SourceName AND [{SourceKeyColumn}] = @SourceKey";

    /// <inheritdoc />
    public string CheckTableExistsSql =>
        $@"SELECT CASE WHEN EXISTS (
               SELECT 1 FROM INFORMATION_SCHEMA.TABLES
               WHERE TABLE_SCHEMA = '{options.Schema}' AND TABLE_NAME = '{options.TableName}'
           ) THEN 1 ELSE 0 END";

    /// <inheritdoc />
    public string CreateTableSql
    {
        get
        {
            var columnType = options.LogFormat == LogFormat.Json
                ? "NVARCHAR(MAX)"
                : "XML";

            return $@"CREATE TABLE {FullTableName} (
    [{LogIdColumn}] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [{SourceNameColumn}] NVARCHAR(64) NOT NULL,
    [{SourceKeyColumn}] NVARCHAR(64) NOT NULL,
    [{AuditLogColumn}] {columnType} NOT NULL,
    CONSTRAINT [PK_{options.TableName}] PRIMARY KEY ([{LogIdColumn}]),
    CONSTRAINT [UQ_{options.TableName}_Source] UNIQUE ([{SourceNameColumn}], [{SourceKeyColumn}])
);";
        }
    }

    /// <inheritdoc />
    public string GetAuditLogColumnTypeSql =>
        $@"SELECT DATA_TYPE
           FROM INFORMATION_SCHEMA.COLUMNS
           WHERE TABLE_SCHEMA = '{options.Schema}'
             AND TABLE_NAME = '{options.TableName}'
             AND COLUMN_NAME = '{AuditLogColumn}'";

    /// <inheritdoc />
    public string ExpectedXmlColumnType => "xml";

    /// <inheritdoc />
    public string ExpectedJsonColumnType => "nvarchar";

    /// <inheritdoc />
    public bool IsXmlCompatibleColumnType(string columnType)
    {
        if (string.IsNullOrEmpty(columnType))
            return false;

        return columnType.Equals("xml", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool IsJsonCompatibleColumnType(string columnType)
    {
        if (string.IsNullOrEmpty(columnType))
            return false;

        return columnType.Equals("nvarchar", StringComparison.OrdinalIgnoreCase) ||
               columnType.Equals("varchar", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public string GetTableStructureSql =>
        $@"SELECT
               COLUMN_NAME AS ColumnName,
               DATA_TYPE AS DataType,
               CHARACTER_MAXIMUM_LENGTH AS MaxLength,
               CASE WHEN IS_NULLABLE = 'YES' THEN 1 ELSE 0 END AS IsNullable
           FROM INFORMATION_SCHEMA.COLUMNS
           WHERE TABLE_SCHEMA = '{options.Schema}'
             AND TABLE_NAME = '{options.TableName}'
           ORDER BY ORDINAL_POSITION";

    /// <inheritdoc />
    public IReadOnlyList<ExpectedColumnDefinition> GetExpectedTableStructure()
    {
        var auditLogType = options.LogFormat == LogFormat.Xml
            ? new[] { "xml" }
            : new[] { "nvarchar", "varchar" };

        var auditLogMinLength = options.LogFormat == LogFormat.Xml
            ? (int?)null
            : -1; // -1 means MAX

        return new List<ExpectedColumnDefinition>
        {
            new()
            {
                ColumnName = LogIdColumn,
                AcceptableDataTypes = ["uniqueidentifier"],
                RequireNotNull = true
            },
            new()
            {
                ColumnName = SourceNameColumn,
                AcceptableDataTypes = ["nvarchar", "varchar"],
                MinLength = 64,
                RequireNotNull = true
            },
            new()
            {
                ColumnName = SourceKeyColumn,
                AcceptableDataTypes = ["nvarchar", "varchar"],
                MinLength = 64, // Max GUID string representation with safety margin
                RequireNotNull = true
            },
            new()
            {
                ColumnName = AuditLogColumn,
                AcceptableDataTypes = auditLogType,
                MinLength = auditLogMinLength,
                RequireNotNull = true
            }
        }.AsReadOnly();
    }

    /// <inheritdoc />
    public bool ValidateColumn(TableColumnInfo actual, ExpectedColumnDefinition expected)
    {
        // Check data type
        var typeMatches = false;
        foreach (var acceptableType in expected.AcceptableDataTypes)
        {
            if (actual.DataType.Equals(acceptableType, StringComparison.OrdinalIgnoreCase))
            {
                typeMatches = true;
                break;
            }
        }

        if (!typeMatches)
            return false;

        // Check length for string types
        if (expected.MinLength.HasValue)
        {
            if (expected.MinLength.Value == -1)
            {
                // Expecting MAX (-1 in SQL Server)
                if (actual.MaxLength.HasValue && actual.MaxLength.Value != -1)
                    return false;
            }
            else if (actual.MaxLength.HasValue && actual.MaxLength.Value < expected.MinLength.Value)
            {
                return false;
            }
        }

        // Check nullability
        if (expected.RequireNotNull && actual.IsNullable)
            return false;

        return true;
    }
}
