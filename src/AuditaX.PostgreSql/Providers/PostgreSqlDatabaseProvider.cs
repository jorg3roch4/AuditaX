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
            var castSuffix = options.ChangeLogFormat == ChangeLogFormat.Json ? "::jsonb" : "::xml";
            return $@"INSERT INTO {FullTableName} (""{LogIdColumn}"", ""{SourceNameColumn}"", ""{SourceKeyColumn}"", ""{AuditLogColumn}"")
           VALUES (@LogId, @SourceName, @SourceKey, @AuditLogXml{castSuffix})";
        }
    }

    /// <inheritdoc />
    public string UpdateSql
    {
        get
        {
            var castSuffix = options.ChangeLogFormat == ChangeLogFormat.Json ? "::jsonb" : "::xml";
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
            var columnType = options.ChangeLogFormat == ChangeLogFormat.Json
                ? "JSONB"
                : "XML";

            return $@"CREATE TABLE IF NOT EXISTS {FullTableName} (
    ""{LogIdColumn}"" UUID NOT NULL DEFAULT gen_random_uuid(),
    ""{SourceNameColumn}"" VARCHAR(50) NOT NULL,
    ""{SourceKeyColumn}"" VARCHAR(900) NOT NULL,
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
        var auditLogType = options.ChangeLogFormat == ChangeLogFormat.Xml
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
                MinLength = 50,
                RequireNotNull = true
            },
            new()
            {
                ColumnName = SourceKeyColumn,
                AcceptableDataTypes = ["character varying", "varchar", "text"],
                MinLength = 128, // Minimum acceptable, we expect 900
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

    #region Query SQL Properties

    /// <inheritdoc />
    public string CreateSourceNameIndexSql =>
        $@"CREATE INDEX IF NOT EXISTS ""ix_{_tableName}_source_name""
           ON {FullTableName} (""{SourceNameColumn}"")
           INCLUDE (""{SourceKeyColumn}"", ""{AuditLogColumn}"")";

    /// <inheritdoc />
    public string GetSelectBySourceNameSql(int skip, int take) =>
        $@"SELECT ""{SourceNameColumn}"" AS ""SourceName"",
                  ""{SourceKeyColumn}"" AS ""SourceKey"",
                  ""{AuditLogColumn}""::text AS ""AuditLog""
           FROM {FullTableName}
           WHERE ""{SourceNameColumn}"" = @SourceName
           ORDER BY ""{SourceKeyColumn}""
           LIMIT @Take OFFSET @Skip";

    /// <inheritdoc />
    public string GetSelectBySourceNameAndDateSql(int skip, int take) =>
        options.ChangeLogFormat == ChangeLogFormat.Json
            ? GetSelectBySourceNameAndDateJsonSql(skip, take)
            : GetSelectBySourceNameAndDateXmlSql(skip, take);

    /// <inheritdoc />
    public string SelectBySourceNameAndActionSql =>
        options.ChangeLogFormat == ChangeLogFormat.Json
            ? SelectBySourceNameAndActionJsonSql
            : SelectBySourceNameAndActionXmlSql;

    /// <inheritdoc />
    public string SelectBySourceNameActionAndDateSql =>
        options.ChangeLogFormat == ChangeLogFormat.Json
            ? SelectBySourceNameActionAndDateJsonSql
            : SelectBySourceNameActionAndDateXmlSql;

    /// <inheritdoc />
    public string GetSelectSummaryBySourceNameSql(int skip, int take) =>
        options.ChangeLogFormat == ChangeLogFormat.Json
            ? GetSelectSummaryBySourceNameJsonSql(skip, take)
            : GetSelectSummaryBySourceNameXmlSql(skip, take);

    #endregion

    #region XML Query Implementations

    private string GetSelectBySourceNameAndDateXmlSql(int skip, int take) =>
        $@"SELECT ""{SourceNameColumn}"" AS ""SourceName"",
                  ""{SourceKeyColumn}"" AS ""SourceKey"",
                  ""{AuditLogColumn}""::text AS ""AuditLog""
           FROM {FullTableName}
           WHERE ""{SourceNameColumn}"" = @SourceName
             AND EXISTS (
                 SELECT 1 FROM unnest(xpath('/AuditLog/Entry/@Timestamp', ""{AuditLogColumn}"")) AS ts
                 WHERE ts::text::timestamp >= @FromDate AND ts::text::timestamp <= @ToDate
             )
           ORDER BY ""{SourceKeyColumn}""
           LIMIT @Take OFFSET @Skip";

    private string SelectBySourceNameAndActionXmlSql =>
        $@"SELECT ""{SourceNameColumn}"" AS ""SourceName"",
                  ""{SourceKeyColumn}"" AS ""SourceKey"",
                  ""{AuditLogColumn}""::text AS ""AuditLog""
           FROM {FullTableName}
           WHERE ""{SourceNameColumn}"" = @SourceName
             AND EXISTS (
                 SELECT 1 FROM unnest(xpath('/AuditLog/Entry/@Action', ""{AuditLogColumn}"")) AS act
                 WHERE act::text = @Action
             )";

    private string SelectBySourceNameActionAndDateXmlSql =>
        $@"SELECT ""{SourceNameColumn}"" AS ""SourceName"",
                  ""{SourceKeyColumn}"" AS ""SourceKey"",
                  ""{AuditLogColumn}""::text AS ""AuditLog""
           FROM {FullTableName}
           WHERE ""{SourceNameColumn}"" = @SourceName
             AND EXISTS (
                 SELECT 1
                 FROM (
                     SELECT unnest(xpath('/AuditLog/Entry/@Action', ""{AuditLogColumn}""))::text AS action,
                            unnest(xpath('/AuditLog/Entry/@Timestamp', ""{AuditLogColumn}""))::text::timestamp AS ts
                 ) entries
                 WHERE entries.action = @Action
                   AND entries.ts >= @FromDate AND entries.ts <= @ToDate
             )";

    private string GetSelectSummaryBySourceNameXmlSql(int skip, int take) =>
        $@"SELECT ""{SourceNameColumn}"" AS ""SourceName"",
                  ""{SourceKeyColumn}"" AS ""SourceKey"",
                  (xpath('/AuditLog/Entry[last()]/@Action', ""{AuditLogColumn}""))[1]::text AS ""LastAction"",
                  (xpath('/AuditLog/Entry[last()]/@Timestamp', ""{AuditLogColumn}""))[1]::text::timestamp AS ""LastTimestamp"",
                  (xpath('/AuditLog/Entry[last()]/@User', ""{AuditLogColumn}""))[1]::text AS ""LastUser""
           FROM {FullTableName}
           WHERE ""{SourceNameColumn}"" = @SourceName
           ORDER BY ""{SourceKeyColumn}""
           LIMIT @Take OFFSET @Skip";

    #endregion

    #region JSON Query Implementations

    private string GetSelectBySourceNameAndDateJsonSql(int skip, int take) =>
        $@"SELECT ""{SourceNameColumn}"" AS ""SourceName"",
                  ""{SourceKeyColumn}"" AS ""SourceKey"",
                  ""{AuditLogColumn}""::text AS ""AuditLog""
           FROM {FullTableName}
           WHERE ""{SourceNameColumn}"" = @SourceName
             AND EXISTS (
                 SELECT 1 FROM jsonb_array_elements(""{AuditLogColumn}""->'auditLog') elem
                 WHERE (elem->>'timestamp')::timestamp >= @FromDate
                   AND (elem->>'timestamp')::timestamp <= @ToDate
             )
           ORDER BY ""{SourceKeyColumn}""
           LIMIT @Take OFFSET @Skip";

    private string SelectBySourceNameAndActionJsonSql =>
        $@"SELECT ""{SourceNameColumn}"" AS ""SourceName"",
                  ""{SourceKeyColumn}"" AS ""SourceKey"",
                  ""{AuditLogColumn}""::text AS ""AuditLog""
           FROM {FullTableName}
           WHERE ""{SourceNameColumn}"" = @SourceName
             AND EXISTS (
                 SELECT 1 FROM jsonb_array_elements(""{AuditLogColumn}""->'auditLog') elem
                 WHERE elem->>'action' = @Action
             )";

    private string SelectBySourceNameActionAndDateJsonSql =>
        $@"SELECT ""{SourceNameColumn}"" AS ""SourceName"",
                  ""{SourceKeyColumn}"" AS ""SourceKey"",
                  ""{AuditLogColumn}""::text AS ""AuditLog""
           FROM {FullTableName}
           WHERE ""{SourceNameColumn}"" = @SourceName
             AND EXISTS (
                 SELECT 1 FROM jsonb_array_elements(""{AuditLogColumn}""->'auditLog') elem
                 WHERE elem->>'action' = @Action
                   AND (elem->>'timestamp')::timestamp >= @FromDate
                   AND (elem->>'timestamp')::timestamp <= @ToDate
             )";

    private string GetSelectSummaryBySourceNameJsonSql(int skip, int take) =>
        $@"SELECT ""{SourceNameColumn}"" AS ""SourceName"",
                  ""{SourceKeyColumn}"" AS ""SourceKey"",
                  (""{AuditLogColumn}""->'auditLog'->-1->>'action') AS ""LastAction"",
                  ((""{AuditLogColumn}""->'auditLog'->-1->>'timestamp')::timestamp) AS ""LastTimestamp"",
                  (""{AuditLogColumn}""->'auditLog'->-1->>'user') AS ""LastUser""
           FROM {FullTableName}
           WHERE ""{SourceNameColumn}"" = @SourceName
           ORDER BY ""{SourceKeyColumn}""
           LIMIT @Take OFFSET @Skip";

    #endregion

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
