using AuditaX.Exceptions;
using FluentAssertions;

namespace AuditaX.Tests.Exceptions;

public class AuditTableStructureMismatchExceptionTests
{
    [Fact]
    public void Constructor_WithMissingColumns_ShouldSetProperties()
    {
        // Arrange
        var tableName = "[dbo].[AuditLog]";
        var missingColumns = new[] { "LogId", "AuditLog" };
        var incorrectColumns = Array.Empty<(string, string, string)>();
        var createTableSql = "CREATE TABLE [dbo].[AuditLog] ...";

        // Act
        var exception = new AuditTableStructureMismatchException(
            tableName,
            missingColumns,
            incorrectColumns,
            createTableSql);

        // Assert
        exception.TableName.Should().Be(tableName);
        exception.MissingColumns.Should().BeEquivalentTo(missingColumns);
        exception.IncorrectColumns.Should().BeEmpty();
        exception.CreateTableSql.Should().Be(createTableSql);
    }

    [Fact]
    public void Constructor_WithIncorrectColumns_ShouldSetProperties()
    {
        // Arrange
        var tableName = "[dbo].[AuditLog]";
        var missingColumns = Array.Empty<string>();
        var incorrectColumns = new[]
        {
            ("SourceName", "nvarchar(50+)", "nvarchar(20)"),
            ("AuditLog", "xml", "nvarchar")
        };
        var createTableSql = "CREATE TABLE [dbo].[AuditLog] ...";

        // Act
        var exception = new AuditTableStructureMismatchException(
            tableName,
            missingColumns,
            incorrectColumns,
            createTableSql);

        // Assert
        exception.TableName.Should().Be(tableName);
        exception.MissingColumns.Should().BeEmpty();
        exception.IncorrectColumns.Should().HaveCount(2);
        exception.IncorrectColumns[0].ColumnName.Should().Be("SourceName");
        exception.IncorrectColumns[0].ExpectedType.Should().Be("nvarchar(50+)");
        exception.IncorrectColumns[0].ActualType.Should().Be("nvarchar(20)");
    }

    [Fact]
    public void Constructor_WithBothIssues_ShouldSetAllProperties()
    {
        // Arrange
        var tableName = "[dbo].[AuditLog]";
        var missingColumns = new[] { "LogId" };
        var incorrectColumns = new[]
        {
            ("SourceName", "nvarchar(50+)", "varchar(128)")
        };
        var createTableSql = "CREATE TABLE [dbo].[AuditLog] ...";

        // Act
        var exception = new AuditTableStructureMismatchException(
            tableName,
            missingColumns,
            incorrectColumns,
            createTableSql);

        // Assert
        exception.MissingColumns.Should().HaveCount(1);
        exception.IncorrectColumns.Should().HaveCount(1);
    }

    [Fact]
    public void Message_WithMissingColumns_ShouldContainMissingColumnNames()
    {
        // Arrange
        var exception = new AuditTableStructureMismatchException(
            "[dbo].[AuditLog]",
            new[] { "LogId", "AuditLog" },
            Array.Empty<(string, string, string)>(),
            "CREATE TABLE ...");

        // Act
        var message = exception.Message;

        // Assert
        message.Should().Contain("LogId");
        message.Should().Contain("AuditLog");
        message.Should().Contain("Missing columns");
    }

    [Fact]
    public void Message_WithIncorrectColumns_ShouldContainColumnDetails()
    {
        // Arrange
        var exception = new AuditTableStructureMismatchException(
            "[dbo].[AuditLog]",
            Array.Empty<string>(),
            new[] { ("SourceName", "nvarchar(50)", "nvarchar(20)") },
            "CREATE TABLE ...");

        // Act
        var message = exception.Message;

        // Assert
        message.Should().Contain("SourceName");
        message.Should().Contain("nvarchar(50)");
        message.Should().Contain("nvarchar(20)");
        message.Should().Contain("incorrect");
    }

    [Fact]
    public void Message_ShouldContainFixInstructions()
    {
        // Arrange
        var exception = new AuditTableStructureMismatchException(
            "[dbo].[AuditLog]",
            new[] { "LogId" },
            Array.Empty<(string, string, string)>(),
            "CREATE TABLE ...");

        // Act
        var message = exception.Message;

        // Assert
        message.Should().Contain("To fix this issue");
        message.Should().Contain("Drop the existing table");
    }

    [Fact]
    public void MissingColumns_ShouldBeReadOnly()
    {
        // Arrange
        var missingColumns = new List<string> { "LogId" };
        var exception = new AuditTableStructureMismatchException(
            "[dbo].[AuditLog]",
            missingColumns,
            Array.Empty<(string, string, string)>(),
            "CREATE TABLE ...");

        // Act - Modify the original list
        missingColumns.Add("SourceName");

        // Assert - Exception's list should not be affected
        exception.MissingColumns.Should().HaveCount(1);
    }

    [Fact]
    public void IncorrectColumns_ShouldBeReadOnly()
    {
        // Arrange
        var incorrectColumns = new List<(string, string, string)>
        {
            ("SourceName", "nvarchar(50)", "varchar(20)")
        };
        var exception = new AuditTableStructureMismatchException(
            "[dbo].[AuditLog]",
            Array.Empty<string>(),
            incorrectColumns,
            "CREATE TABLE ...");

        // Act - Modify the original list
        incorrectColumns.Add(("AuditLog", "xml", "nvarchar"));

        // Assert - Exception's list should not be affected
        exception.IncorrectColumns.Should().HaveCount(1);
    }

    [Fact]
    public void SimulateOldIncorrectSchema_ShouldIdentifyAllIssues()
    {
        // This test simulates what would happen with the old incorrect schema
        // Old schema: AuditLogId INT, SourceName NVARCHAR(128), SourceKey NVARCHAR(128),
        //             Action NVARCHAR(16), Changes NVARCHAR(MAX), User NVARCHAR(128), Timestamp DATETIME2
        // Expected:   LogId UNIQUEIDENTIFIER, SourceName NVARCHAR(50), SourceKey NVARCHAR(900), AuditLog XML/NVARCHAR(MAX)

        var missingColumns = new[] { "LogId", "AuditLog" }; // These don't exist in old schema
        var incorrectColumns = Array.Empty<(string, string, string)>(); // SourceName/SourceKey might match if longer

        var exception = new AuditTableStructureMismatchException(
            "[dbo].[AuditLog]",
            missingColumns,
            incorrectColumns,
            "CREATE TABLE [dbo].[AuditLog] (...)");

        // Assert
        exception.MissingColumns.Should().Contain("LogId");
        exception.MissingColumns.Should().Contain("AuditLog");
        exception.Message.Should().Contain("LogId");
        exception.Message.Should().Contain("AuditLog");
    }
}
