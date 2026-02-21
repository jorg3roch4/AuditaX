using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moq;
using AuditaX.Configuration;
using AuditaX.Entities;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.EntityFramework.Contexts;

namespace AuditaX.EntityFramework.Tests.Contexts;

public class AuditaXDbContextTests
{
    [Fact]
    public void OnModelCreating_SqlServer_Xml_ShouldConfigureColumnNames()
    {
        var providerMock = CreateSqlServerProviderMock();
        var options = new AuditaXOptions { TableName = "AuditLog", Schema = "dbo", LogFormat = LogFormat.Xml };

        using var dbContext = CreateDbContext(options, providerMock.Object);

        var entityType = dbContext.Model.FindEntityType(typeof(AuditLog));
        entityType.Should().NotBeNull();

        var tableName = entityType!.GetTableName();
        tableName.Should().Be("AuditLog");

        var schema = entityType.GetSchema();
        schema.Should().Be("dbo");
    }

    [Fact]
    public void OnModelCreating_SqlServer_ShouldMapColumnNames()
    {
        var providerMock = CreateSqlServerProviderMock();
        var options = new AuditaXOptions { TableName = "AuditLog", Schema = "dbo", LogFormat = LogFormat.Xml };

        using var dbContext = CreateDbContext(options, providerMock.Object);

        var entityType = dbContext.Model.FindEntityType(typeof(AuditLog))!;
        var logIdProp = entityType.FindProperty(nameof(AuditLog.LogId))!;
        var sourceNameProp = entityType.FindProperty(nameof(AuditLog.SourceName))!;
        var sourceKeyProp = entityType.FindProperty(nameof(AuditLog.SourceKey))!;
        var auditLogProp = entityType.FindProperty(nameof(AuditLog.AuditLogXml))!;

        logIdProp.GetColumnName().Should().Be("LogId");
        sourceNameProp.GetColumnName().Should().Be("SourceName");
        sourceKeyProp.GetColumnName().Should().Be("SourceKey");
        auditLogProp.GetColumnName().Should().Be("AuditLog");
    }

    [Fact]
    public void OnModelCreating_PostgreSql_ShouldMapSnakeCaseColumnNames()
    {
        var providerMock = CreatePostgreSqlProviderMock();
        var options = new AuditaXOptions { TableName = "AuditLog", Schema = "public", LogFormat = LogFormat.Json };

        using var dbContext = CreateDbContext(options, providerMock.Object);

        var entityType = dbContext.Model.FindEntityType(typeof(AuditLog))!;
        var logIdProp = entityType.FindProperty(nameof(AuditLog.LogId))!;

        logIdProp.GetColumnName().Should().Be("log_id");
    }

    [Fact]
    public void OnModelCreating_SqlServer_Xml_ShouldUseXmlColumnType()
    {
        var providerMock = CreateSqlServerProviderMock();
        var options = new AuditaXOptions { TableName = "AuditLog", Schema = "dbo", LogFormat = LogFormat.Xml };

        using var dbContext = CreateDbContext(options, providerMock.Object);

        var entityType = dbContext.Model.FindEntityType(typeof(AuditLog))!;
        var auditLogProp = entityType.FindProperty(nameof(AuditLog.AuditLogXml))!;

        // Use annotation instead of GetColumnType() which requires relational type mapping (not available in InMemory)
        var columnType = auditLogProp.FindAnnotation("Relational:ColumnType")?.Value as string;
        columnType.Should().Be("XML");
    }

    [Fact]
    public void OnModelCreating_SqlServer_Json_ShouldUseNvarcharMaxColumnType()
    {
        var providerMock = CreateSqlServerProviderMock();
        var options = new AuditaXOptions { TableName = "AuditLog", Schema = "dbo", LogFormat = LogFormat.Json };

        using var dbContext = CreateDbContext(options, providerMock.Object);

        var entityType = dbContext.Model.FindEntityType(typeof(AuditLog))!;
        var auditLogProp = entityType.FindProperty(nameof(AuditLog.AuditLogXml))!;

        var columnType = auditLogProp.FindAnnotation("Relational:ColumnType")?.Value as string;
        columnType.Should().Be("NVARCHAR(MAX)");
    }

    [Fact]
    public void OnModelCreating_PostgreSql_Json_ShouldUseJsonbColumnType()
    {
        var providerMock = CreatePostgreSqlProviderMock();
        var options = new AuditaXOptions { TableName = "AuditLog", Schema = "public", LogFormat = LogFormat.Json };

        using var dbContext = CreateDbContext(options, providerMock.Object);

        var entityType = dbContext.Model.FindEntityType(typeof(AuditLog))!;
        var auditLogProp = entityType.FindProperty(nameof(AuditLog.AuditLogXml))!;

        var columnType = auditLogProp.FindAnnotation("Relational:ColumnType")?.Value as string;
        columnType.Should().Be("JSONB");
    }

    [Fact]
    public void OnModelCreating_ShouldCreateUniqueIndex()
    {
        var providerMock = CreateSqlServerProviderMock();
        var options = new AuditaXOptions { TableName = "AuditLog", Schema = "dbo", LogFormat = LogFormat.Xml };

        using var dbContext = CreateDbContext(options, providerMock.Object);

        var entityType = dbContext.Model.FindEntityType(typeof(AuditLog))!;
        var indexes = entityType.GetIndexes().ToList();

        indexes.Should().ContainSingle(i => i.IsUnique);
    }

    #region Helper Methods

    private static AuditaXDbContext CreateDbContext(AuditaXOptions options, IDatabaseProvider provider)
    {
        var dbContextOptions = new DbContextOptionsBuilder<AuditaXDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableServiceProviderCaching(false)
            .Options;

        return new AuditaXDbContext(dbContextOptions, options, provider);
    }

    private static Mock<IDatabaseProvider> CreateSqlServerProviderMock()
    {
        var mock = new Mock<IDatabaseProvider>();
        mock.Setup(p => p.LogIdColumn).Returns("LogId");
        mock.Setup(p => p.SourceNameColumn).Returns("SourceName");
        mock.Setup(p => p.SourceKeyColumn).Returns("SourceKey");
        mock.Setup(p => p.AuditLogColumn).Returns("AuditLog");
        return mock;
    }

    private static Mock<IDatabaseProvider> CreatePostgreSqlProviderMock()
    {
        var mock = new Mock<IDatabaseProvider>();
        mock.Setup(p => p.LogIdColumn).Returns("log_id");
        mock.Setup(p => p.SourceNameColumn).Returns("source_name");
        mock.Setup(p => p.SourceKeyColumn).Returns("source_key");
        mock.Setup(p => p.AuditLogColumn).Returns("audit_log");
        return mock;
    }

    #endregion
}
