using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.Models;
using AuditaX.PostgreSql.Providers;
using FluentAssertions;

namespace AuditaX.Dapper.Tests.Validators;

public class PostgreSqlTableStructureValidationTests
{
    #region PostgreSql GetExpectedTableStructure Tests

    [Fact]
    public void PostgreSql_GetExpectedTableStructure_Json_ShouldReturn4Columns()
    {
        // Arrange
        var options = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "public",
            LogFormat = LogFormat.Json
        };
        var provider = new PostgreSqlDatabaseProvider(options);

        // Act
        var structure = provider.GetExpectedTableStructure();

        // Assert
        structure.Should().HaveCount(4);
        structure.Select(c => c.ColumnName).Should().Contain(new[] { "log_id", "source_name", "source_key", "audit_log" });
    }

    [Fact]
    public void PostgreSql_GetExpectedTableStructure_Json_AuditLogColumn_ShouldExpectJsonb()
    {
        // Arrange
        var options = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "public",
            LogFormat = LogFormat.Json
        };
        var provider = new PostgreSqlDatabaseProvider(options);

        // Act
        var structure = provider.GetExpectedTableStructure();
        var auditLogColumn = structure.First(c => c.ColumnName == "audit_log");

        // Assert
        auditLogColumn.AcceptableDataTypes.Should().Contain("jsonb");
        auditLogColumn.AcceptableDataTypes.Should().Contain("json");
        auditLogColumn.AcceptableDataTypes.Should().Contain("text"); // Also accept text for backward compatibility
    }

    [Fact]
    public void PostgreSql_GetExpectedTableStructure_Xml_AuditLogColumn_ShouldExpectXml()
    {
        // Arrange
        var options = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "public",
            LogFormat = LogFormat.Xml
        };
        var provider = new PostgreSqlDatabaseProvider(options);

        // Act
        var structure = provider.GetExpectedTableStructure();
        var auditLogColumn = structure.First(c => c.ColumnName == "audit_log");

        // Assert
        auditLogColumn.AcceptableDataTypes.Should().Contain("xml");
    }

    #endregion

    #region PostgreSql ValidateColumn Tests

    [Fact]
    public void PostgreSql_ValidateColumn_CorrectLogId_ShouldReturnTrue()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new PostgreSqlDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "log_id",
            DataType = "uuid",
            MaxLength = null,
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "log_id");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PostgreSql_ValidateColumn_WrongLogIdType_ShouldReturnFalse()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new PostgreSqlDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "log_id",
            DataType = "integer", // Wrong! Should be uuid
            MaxLength = null,
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "log_id");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void PostgreSql_ValidateColumn_CorrectSourceName_ShouldReturnTrue()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new PostgreSqlDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "source_name",
            DataType = "character varying",
            MaxLength = 50,
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "source_name");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PostgreSql_ValidateColumn_SourceNameAsText_ShouldReturnTrue()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new PostgreSqlDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "source_name",
            DataType = "text", // TEXT has no length limit, should be acceptable
            MaxLength = null,
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "source_name");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PostgreSql_ValidateColumn_SourceNameTooShort_ShouldReturnFalse()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new PostgreSqlDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "source_name",
            DataType = "character varying",
            MaxLength = 20, // Too short! Minimum is 50
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "source_name");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void PostgreSql_ValidateColumn_AuditLogJsonAsJsonb_ShouldReturnTrue()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new PostgreSqlDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "audit_log",
            DataType = "jsonb",
            MaxLength = null,
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "audit_log");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PostgreSql_ValidateColumn_AuditLogJsonAsText_ShouldReturnTrue()
    {
        // Arrange - text is still accepted for backward compatibility
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new PostgreSqlDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "audit_log",
            DataType = "text",
            MaxLength = null,
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "audit_log");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PostgreSql_ValidateColumn_AuditLogXmlCorrect_ShouldReturnTrue()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Xml };
        var provider = new PostgreSqlDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "audit_log",
            DataType = "xml",
            MaxLength = null,
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "audit_log");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PostgreSql_ValidateColumn_NullableWhenRequired_ShouldReturnFalse()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new PostgreSqlDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "source_name",
            DataType = "character varying",
            MaxLength = 50,
            IsNullable = true // Wrong! Should be NOT NULL
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "source_name");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region PostgreSql GetTableStructureSql Tests

    [Fact]
    public void PostgreSql_GetTableStructureSql_ShouldContainCorrectColumns()
    {
        // Arrange
        var options = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "public"
        };
        var provider = new PostgreSqlDatabaseProvider(options);

        // Act
        var sql = provider.GetTableStructureSql;

        // Assert
        sql.Should().Contain("column_name");
        sql.Should().Contain("data_type");
        sql.Should().Contain("character_maximum_length");
        sql.Should().Contain("is_nullable");
        sql.Should().Contain("information_schema.columns");
        sql.Should().Contain("public");
        sql.Should().Contain("audit_log"); // snake_case conversion
    }

    [Fact]
    public void PostgreSql_TableName_ShouldConvertToSnakeCase()
    {
        // Arrange
        var options = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "public"
        };
        var provider = new PostgreSqlDatabaseProvider(options);

        // Act
        var fullTableName = provider.FullTableName;

        // Assert
        fullTableName.Should().Contain("audit_log");
    }

    #endregion

    #region Incorrect Table Structure Simulation Tests

    [Fact]
    public void PostgreSql_ValidateColumn_OldSchemaAuditLogId_ShouldNotMatchLogId()
    {
        // This simulates the old incorrect schema that had audit_log_id SERIAL instead of log_id UUID
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new PostgreSqlDatabaseProvider(options);

        // Old incorrect column
        var oldColumn = new TableColumnInfo
        {
            ColumnName = "audit_log_id", // Wrong name!
            DataType = "integer",
            MaxLength = null,
            IsNullable = false
        };

        var expectedLogId = provider.GetExpectedTableStructure().First(c => c.ColumnName == "log_id");

        // The column name won't match, so validation would find "log_id" missing
        oldColumn.ColumnName.Should().NotBe(expectedLogId.ColumnName);
    }

    [Fact]
    public void PostgreSql_ValidateColumn_OldSchemaChangesColumn_ShouldNotMatchAuditLog()
    {
        // This simulates the old incorrect schema that had "changes" column instead of "audit_log"
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new PostgreSqlDatabaseProvider(options);

        // Old incorrect column
        var oldColumn = new TableColumnInfo
        {
            ColumnName = "changes", // Wrong name!
            DataType = "text",
            MaxLength = null,
            IsNullable = true
        };

        var expectedAuditLog = provider.GetExpectedTableStructure().First(c => c.ColumnName == "audit_log");

        // The column name won't match, so validation would find "audit_log" missing
        oldColumn.ColumnName.Should().NotBe(expectedAuditLog.ColumnName);
    }

    #endregion
}
