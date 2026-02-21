using AuditaX.Exceptions;

namespace AuditaX.Tests.Exceptions;

public class AuditTableNotFoundExceptionTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var tableName = "[dbo].[AuditLog]";
        var createTableSql = "CREATE TABLE [dbo].[AuditLog] (...)";

        var exception = new AuditTableNotFoundException(tableName, createTableSql);

        exception.TableName.Should().Be(tableName);
        exception.CreateTableSql.Should().Be(createTableSql);
    }

    [Fact]
    public void Message_ShouldContainTableNameAndSql()
    {
        var exception = new AuditTableNotFoundException(
            "[dbo].[AuditLog]",
            "CREATE TABLE [dbo].[AuditLog] (...)");

        exception.Message.Should().Contain("[dbo].[AuditLog]");
        exception.Message.Should().Contain("does not exist");
        exception.Message.Should().Contain("CREATE TABLE");
        exception.Message.Should().Contain("AutoCreateTable");
    }

    [Fact]
    public void ConstructorWithInnerException_ShouldSetInnerException()
    {
        var innerException = new InvalidOperationException("Connection failed");
        var exception = new AuditTableNotFoundException(
            "AuditLog",
            "CREATE TABLE ...",
            innerException);

        exception.InnerException.Should().Be(innerException);
        exception.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void ConstructorWithInnerException_ShouldStillSetProperties()
    {
        var innerException = new Exception("test");
        var exception = new AuditTableNotFoundException(
            "MyTable",
            "CREATE TABLE MyTable (...)",
            innerException);

        exception.TableName.Should().Be("MyTable");
        exception.CreateTableSql.Should().Be("CREATE TABLE MyTable (...)");
        exception.Message.Should().Contain("MyTable");
    }
}
