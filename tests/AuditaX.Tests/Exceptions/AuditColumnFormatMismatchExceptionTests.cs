using AuditaX.Enums;
using AuditaX.Exceptions;

namespace AuditaX.Tests.Exceptions;

public class AuditColumnFormatMismatchExceptionTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var exception = new AuditColumnFormatMismatchException(
            "[dbo].[AuditLog]",
            "AuditLog",
            LogFormat.Json,
            "nvarchar",
            "xml");

        exception.TableName.Should().Be("[dbo].[AuditLog]");
        exception.ColumnName.Should().Be("AuditLog");
        exception.ExpectedFormat.Should().Be(LogFormat.Json);
        exception.ExpectedColumnType.Should().Be("nvarchar");
        exception.ActualColumnType.Should().Be("xml");
    }

    [Fact]
    public void Message_ShouldContainRelevantInfo()
    {
        var exception = new AuditColumnFormatMismatchException(
            "[dbo].[AuditLog]",
            "AuditLog",
            LogFormat.Json,
            "nvarchar",
            "xml");

        exception.Message.Should().Contain("[dbo].[AuditLog]");
        exception.Message.Should().Contain("AuditLog");
        exception.Message.Should().Contain("Json");
        exception.Message.Should().Contain("nvarchar");
        exception.Message.Should().Contain("xml");
        exception.Message.Should().Contain("mismatch");
    }

    [Fact]
    public void ConstructorWithInnerException_ShouldSetInnerException()
    {
        var innerException = new InvalidOperationException("DB error");
        var exception = new AuditColumnFormatMismatchException(
            "AuditLog",
            "AuditLog",
            LogFormat.Xml,
            "xml",
            "nvarchar",
            innerException);

        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void ConstructorWithInnerException_ShouldSetAllProperties()
    {
        var innerException = new Exception("test");
        var exception = new AuditColumnFormatMismatchException(
            "MyTable",
            "MyColumn",
            LogFormat.Xml,
            "xml",
            "jsonb",
            innerException);

        exception.TableName.Should().Be("MyTable");
        exception.ColumnName.Should().Be("MyColumn");
        exception.ExpectedFormat.Should().Be(LogFormat.Xml);
        exception.ExpectedColumnType.Should().Be("xml");
        exception.ActualColumnType.Should().Be("jsonb");
    }

    [Fact]
    public void Message_ShouldContainFixInstructions()
    {
        var exception = new AuditColumnFormatMismatchException(
            "AuditLog",
            "AuditLog",
            LogFormat.Json,
            "nvarchar",
            "xml");

        exception.Message.Should().Contain("Change the configuration");
        exception.Message.Should().Contain("Recreate the audit table");
    }
}
