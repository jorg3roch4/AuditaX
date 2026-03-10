using System;
using System.Collections.Generic;
using System.Text;
using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;

namespace AuditaX.PostgreSql.Providers;

/// <summary>
/// PostgreSQL database provider implementation.
/// </summary>
/// <param name="options">The audit options.</param>
public sealed class PostgreSqlDatabaseProvider(AuditaXOptions options) : IDatabaseProvider
{
    private readonly string _tableName = ToSnakeCase(options.TableName);
    private readonly string _schema = options.Schema.ToLowerInvariant();

    /// <inheritdoc />
    public string FullTableName => $"\"{_schema}\".\"{_tableName}\"";

    /// <inheritdoc />
    public string LogIdColumn => "log_id";

    /// <inheritdoc />
    public string SourceNameColumn => "source_name";

    /// <inheritdoc />
    public string SourceKeyColumn => "source_key";

    /// <inheritdoc />
    public string AuditLogColumn => "audit_log";

    /// <inheritdoc />
    public string SelectByEntitySql =>
        $@"SELECT ""{LogIdColumn}"" AS ""LogId"",
                  ""{SourceNameColumn}"" AS ""SourceName"",
                  ""{SourceKeyColumn}"" AS ""SourceKey"",
                  ""{AuditLogColumn}""::text AS ""AuditLogXml""
           FROM {FullTableName}
           WHERE ""{SourceNameColumn}"" = @SourceName AND ""{SourceKeyColumn}"" = @SourceKey";

    /// <inheritdoc />
    public string InsertSql
    {
        get
        {
            var castSuffix = options.LogFormat == LogFormat.Json ? "::jsonb" : "::xml";
            return $@"INSERT INTO {FullTableName} (""{LogIdColumn}"", ""{SourceNameColumn}"", ""{SourceKeyColumn}"", ""{AuditLogColumn}"")
           VALUES (@LogId, @SourceName, @SourceKey, @AuditLogXml{castSuffix})";
        }
    }

    /// <inheritdoc />
    public string UpdateSql
    {
        get
        {
            var castSuffix = options.LogFormat == LogFormat.Json ? "::jsonb" : "::xml";
            return $@"UPDATE {FullTableName}
           SET ""{AuditLogColumn}"" = @AuditLogXml{castSuffix}
           WHERE ""{SourceNameColumn}"" = @SourceName AND ""{SourceKeyColumn}"" = @SourceKey";
        }
    }

    /// <inheritdoc />
    public string CheckTableExistsSql =>
        $@"SELECT CASE WHEN EXISTS (
               SELECT 1 FROM information_schema.tables
               WHERE table_schema = '{_schema}' AND table_name = '{_tableName}'
           ) THEN 1 ELSE 0 END";

    /// <inheritdoc />
    public string CreateTableSql
    {
        get
        {
            var columnType = options.LogFormat == LogFormat.Json
                ? "JSONB"
                : "XML";

            return $@"CREATE TABLE IF NOT EXISTS {FullTableName} (
    ""{LogIdColumn}"" UUID NOT NULL DEFAULT gen_random_uuid(),
    ""{SourceNameColumn}"" VARCHAR(64) NOT NULL,
    ""{SourceKeyColumn}"" VARCHAR(64) NOT NULL,
    ""{AuditLogColumn}"" {columnType} NOT NULL,
    CONSTRAINT ""pk_{_tableName}"" PRIMARY KEY (""{LogIdColumn}""),
    CONSTRAINT ""uq_{_tableName}_source"" UNIQUE (""{SourceNameColumn}"", ""{SourceKeyColumn}"")
);";
        }
    }

    /// <inheritdoc />
    public string GetAuditLogColumnTypeSql =>
        $@"SELECT data_type
           FROM information_schema.columns
           WHERE table_schema = '{_schema}'
             AND table_name = '{_tableName}'
             AND column_name = '{AuditLogColumn}'";

    /// <inheritdoc />
    public string ExpectedXmlColumnType => "xml";

    /// <inheritdoc />
    public string ExpectedJsonColumnType => "jsonb";

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

        return columnType.Equals("jsonb", StringComparison.OrdinalIgnoreCase) ||
               columnType.Equals("json", StringComparison.OrdinalIgnoreCase) ||
               columnType.Equals("text", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public string GetTableStructureSql =>
        $@"SELECT
               column_name AS ""ColumnName"",
               data_type AS ""DataType"",
               character_maximum_length AS ""MaxLength"",
               CASE WHEN is_nullable = 'YES' THEN true ELSE false END AS ""IsNullable""
           FROM information_schema.columns
           WHERE table_schema = '{_schema}'
             AND table_name = '{_tableName}'
           ORDER BY ordinal_position";

    /// <inheritdoc />
    public IReadOnlyList<ExpectedColumnDefinition> GetExpectedTableStructure()
    {
        var auditLogType = options.LogFormat == LogFormat.Xml
            ? new[] { "xml" }
            : new[] { "jsonb", "json", "text" };

        return new List<ExpectedColumnDefinition>
        {
            new()
            {
                ColumnName = LogIdColumn,
                AcceptableDataTypes = ["uuid"],
                RequireNotNull = true
            },
            new()
            {
                ColumnName = SourceNameColumn,
                AcceptableDataTypes = ["character varying", "varchar", "text"],
                MinLength = 64,
                RequireNotNull = true
            },
            new()
            {
                ColumnName = SourceKeyColumn,
                AcceptableDataTypes = ["character varying", "varchar", "text"],
                MinLength = 64, // Max GUID string representation with safety margin
                RequireNotNull = true
            },
            new()
            {
                ColumnName = AuditLogColumn,
                AcceptableDataTypes = auditLogType,
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

        // Check length for string types (text type has no max length in PostgreSQL)
        if (expected.MinLength.HasValue && !actual.DataType.Equals("text", StringComparison.OrdinalIgnoreCase))
        {
            if (actual.MaxLength.HasValue && actual.MaxLength.Value < expected.MinLength.Value)
            {
                return false;
            }
        }

        // Check nullability
        if (expected.RequireNotNull && actual.IsNullable)
            return false;

        return true;
    }

    private static string ToSnakeCase(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var result = new StringBuilder();
        result.Append(char.ToLowerInvariant(text[0]));

        for (int i = 1; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsUpper(c))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }
}
