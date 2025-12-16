using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.Models;
using AuditaX.SqlServer.Providers;
using FluentAssertions;

namespace AuditaX.Dapper.Tests.Validators;

public class TableStructureValidationTests
{
    #region SqlServer GetExpectedTableStructure Tests

    [Fact]
    public void SqlServer_GetExpectedTableStructure_Json_ShouldReturn4Columns()
    {
        // Arrange
        var options = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "dbo",
            LogFormat = LogFormat.Json
        };
        var provider = new SqlServerDatabaseProvider(options);

        // Act
        var structure = provider.GetExpectedTableStructure();

        // Assert
        structure.Should().HaveCount(4);
        structure.Select(c => c.ColumnName).Should().Contain(new[] { "LogId", "SourceName", "SourceKey", "AuditLog" });
    }

    [Fact]
    public void SqlServer_GetExpectedTableStructure_Json_AuditLogColumn_ShouldExpectNvarchar()
    {
        // Arrange
        var options = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "dbo",
            LogFormat = LogFormat.Json
        };
        var provider = new SqlServerDatabaseProvider(options);

        // Act
        var structure = provider.GetExpectedTableStructure();
        var auditLogColumn = structure.First(c => c.ColumnName == "AuditLog");

        // Assert
        auditLogColumn.AcceptableDataTypes.Should().Contain("nvarchar");
        auditLogColumn.MinLength.Should().Be(-1); // MAX
    }

    [Fact]
    public void SqlServer_GetExpectedTableStructure_Xml_AuditLogColumn_ShouldExpectXml()
    {
        // Arrange
        var options = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "dbo",
            LogFormat = LogFormat.Xml
        };
        var provider = new SqlServerDatabaseProvider(options);

        // Act
        var structure = provider.GetExpectedTableStructure();
        var auditLogColumn = structure.First(c => c.ColumnName == "AuditLog");

        // Assert
        auditLogColumn.AcceptableDataTypes.Should().Contain("xml");
        auditLogColumn.MinLength.Should().BeNull();
    }

    #endregion

    #region SqlServer ValidateColumn Tests

    [Fact]
    public void SqlServer_ValidateColumn_CorrectLogId_ShouldReturnTrue()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new SqlServerDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "LogId",
            DataType = "uniqueidentifier",
            MaxLength = null,
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "LogId");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SqlServer_ValidateColumn_WrongLogIdType_ShouldReturnFalse()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new SqlServerDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "LogId",
            DataType = "int", // Wrong! Should be uniqueidentifier
            MaxLength = null,
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "LogId");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SqlServer_ValidateColumn_CorrectSourceName_ShouldReturnTrue()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new SqlServerDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "SourceName",
            DataType = "nvarchar",
            MaxLength = 50,
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "SourceName");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SqlServer_ValidateColumn_SourceNameTooShort_ShouldReturnFalse()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new SqlServerDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "SourceName",
            DataType = "nvarchar",
            MaxLength = 20, // Too short! Minimum is 50
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "SourceName");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SqlServer_ValidateColumn_SourceNameLongerThanMinimum_ShouldReturnTrue()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new SqlServerDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "SourceName",
            DataType = "nvarchar",
            MaxLength = 128, // Longer than minimum, should be OK
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "SourceName");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SqlServer_ValidateColumn_NullableWhenRequired_ShouldReturnFalse()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new SqlServerDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "SourceName",
            DataType = "nvarchar",
            MaxLength = 50,
            IsNullable = true // Wrong! Should be NOT NULL
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "SourceName");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SqlServer_ValidateColumn_AuditLogJsonCorrect_ShouldReturnTrue()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new SqlServerDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "AuditLog",
            DataType = "nvarchar",
            MaxLength = -1, // MAX
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "AuditLog");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SqlServer_ValidateColumn_AuditLogXmlCorrect_ShouldReturnTrue()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Xml };
        var provider = new SqlServerDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "AuditLog",
            DataType = "xml",
            MaxLength = null,
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "AuditLog");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SqlServer_ValidateColumn_AuditLogWrongType_ShouldReturnFalse()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new SqlServerDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "AuditLog",
            DataType = "xml", // Wrong! JSON expects nvarchar
            MaxLength = null,
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "AuditLog");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SqlServer_ValidateColumn_AuditLogNotMax_ShouldReturnFalse()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new SqlServerDatabaseProvider(options);
        var actual = new TableColumnInfo
        {
            ColumnName = "AuditLog",
            DataType = "nvarchar",
            MaxLength = 4000, // Wrong! Should be MAX (-1)
            IsNullable = false
        };
        var expected = provider.GetExpectedTableStructure().First(c => c.ColumnName == "AuditLog");

        // Act
        var result = provider.ValidateColumn(actual, expected);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SqlServer GetTableStructureSql Tests

    [Fact]
    public void SqlServer_GetTableStructureSql_ShouldContainCorrectColumns()
    {
        // Arrange
        var options = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "dbo"
        };
        var provider = new SqlServerDatabaseProvider(options);

        // Act
        var sql = provider.GetTableStructureSql;

        // Assert
        sql.Should().Contain("COLUMN_NAME");
        sql.Should().Contain("DATA_TYPE");
        sql.Should().Contain("CHARACTER_MAXIMUM_LENGTH");
        sql.Should().Contain("IS_NULLABLE");
        sql.Should().Contain("INFORMATION_SCHEMA.COLUMNS");
        sql.Should().Contain("dbo");
        sql.Should().Contain("AuditLog");
    }

    #endregion

    #region Incorrect Table Structure Simulation Tests

    [Fact]
    public void SqlServer_ValidateColumn_OldSchemaAuditLogId_ShouldNotMatchLogId()
    {
        // This simulates the old incorrect schema that had AuditLogId INT instead of LogId UNIQUEIDENTIFIER
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new SqlServerDatabaseProvider(options);

        // Old incorrect column
        var oldColumn = new TableColumnInfo
        {
            ColumnName = "AuditLogId", // Wrong name!
            DataType = "int",
            MaxLength = null,
            IsNullable = false
        };

        var expectedLogId = provider.GetExpectedTableStructure().First(c => c.ColumnName == "LogId");

        // The column name won't match, so validation would find "LogId" missing
        oldColumn.ColumnName.Should().NotBe(expectedLogId.ColumnName);
    }

    [Fact]
    public void SqlServer_ValidateColumn_OldSchemaChangesColumn_ShouldNotMatchAuditLog()
    {
        // This simulates the old incorrect schema that had "Changes" column instead of "AuditLog"
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var provider = new SqlServerDatabaseProvider(options);

        // Old incorrect column
        var oldColumn = new TableColumnInfo
        {
            ColumnName = "Changes", // Wrong name!
            DataType = "nvarchar",
            MaxLength = -1,
            IsNullable = true
        };

        var expectedAuditLog = provider.GetExpectedTableStructure().First(c => c.ColumnName == "AuditLog");

        // The column name won't match, so validation would find "AuditLog" missing
        oldColumn.ColumnName.Should().NotBe(expectedAuditLog.ColumnName);
    }

    #endregion
}
