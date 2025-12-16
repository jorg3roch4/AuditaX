using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.Models;
using AuditaX.Services;

namespace AuditaX.Tests.Services;

public class ChangeLogServiceTests
{
    [Fact]
    public void DefaultFormat_ShouldBeXml()
    {
        // Arrange
        var options = new AuditaXOptions();
        var service = new ChangeLogService(options);

        // Act
        var result = service.CreateEntry(null, "test@example.com");

        // Assert
        result.Should().StartWith("<AuditLog>");
    }

    [Fact]
    public void JsonFormat_ShouldGenerateJson()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var service = new ChangeLogService(options);

        // Act
        var result = service.CreateEntry(null, "test@example.com");

        // Assert
        result.Should().StartWith("{\"auditLog\"");
    }

    [Fact]
    public void AutoDetect_ShouldParseXml()
    {
        // Arrange
        var xmlOptions = new AuditaXOptions { LogFormat = LogFormat.Xml };
        var xmlService = new ChangeLogService(xmlOptions);
        var xml = xmlService.CreateEntry(null, "test@example.com");

        // Use a different service configured for JSON to test auto-detection
        var jsonOptions = new AuditaXOptions { LogFormat = LogFormat.Json };
        var jsonService = new ChangeLogService(jsonOptions);

        // Act
        var entries = jsonService.ParseAuditLog(xml);

        // Assert
        entries.Should().HaveCount(1);
        entries[0].Action.Should().Be(AuditAction.Created);
    }

    [Fact]
    public void AutoDetect_ShouldParseJson()
    {
        // Arrange
        var jsonOptions = new AuditaXOptions { LogFormat = LogFormat.Json };
        var jsonService = new ChangeLogService(jsonOptions);
        var json = jsonService.CreateEntry(null, "test@example.com");

        // Use a different service configured for XML to test auto-detection
        var xmlOptions = new AuditaXOptions { LogFormat = LogFormat.Xml };
        var xmlService = new ChangeLogService(xmlOptions);

        // Act
        var entries = xmlService.ParseAuditLog(json);

        // Assert
        entries.Should().HaveCount(1);
        entries[0].Action.Should().Be(AuditAction.Created);
    }

    [Fact]
    public void PreserveFormat_WhenAppendingToXml_ShouldStayXml()
    {
        // Arrange - Create initial XML entry
        var xmlOptions = new AuditaXOptions { LogFormat = LogFormat.Xml };
        var xmlService = new ChangeLogService(xmlOptions);
        var xml = xmlService.CreateEntry(null, "first@example.com");

        // Create a service configured for JSON
        var jsonOptions = new AuditaXOptions { LogFormat = LogFormat.Json };
        var jsonService = new ChangeLogService(jsonOptions);

        // Act - Append using JSON-configured service
        var result = jsonService.UpdateEntry(xml,
        [
            new() { Name = "Price", Before = "100", After = "200" }
        ], "second@example.com");

        // Assert - Should still be XML because we're appending to XML
        result.Should().StartWith("<AuditLog>");
        result.Should().Contain("first@example.com");
        result.Should().Contain("second@example.com");
    }

    [Fact]
    public void PreserveFormat_WhenAppendingToJson_ShouldStayJson()
    {
        // Arrange - Create initial JSON entry
        var jsonOptions = new AuditaXOptions { LogFormat = LogFormat.Json };
        var jsonService = new ChangeLogService(jsonOptions);
        var json = jsonService.CreateEntry(null, "first@example.com");

        // Create a service configured for XML
        var xmlOptions = new AuditaXOptions { LogFormat = LogFormat.Xml };
        var xmlService = new ChangeLogService(xmlOptions);

        // Act - Append using XML-configured service
        var result = xmlService.UpdateEntry(json,
        [
            new() { Name = "Price", Before = "100", After = "200" }
        ], "second@example.com");

        // Assert - Should still be JSON because we're appending to JSON
        result.Should().StartWith("{\"auditLog\"");
        result.Should().Contain("first@example.com");
        result.Should().Contain("second@example.com");
    }

    [Fact]
    public void DetectFormatType_ShouldDetectXml()
    {
        // Act & Assert
        ChangeLogService.DetectFormatType("<AuditLog>").Should().Be(LogFormat.Xml);
        ChangeLogService.DetectFormatType("  <AuditLog>").Should().Be(LogFormat.Xml);
    }

    [Fact]
    public void DetectFormatType_ShouldDetectJson()
    {
        // Act & Assert
        ChangeLogService.DetectFormatType("{\"auditLog\":[]}").Should().Be(LogFormat.Json);
        ChangeLogService.DetectFormatType("  {\"auditLog\":[]}").Should().Be(LogFormat.Json);
        ChangeLogService.DetectFormatType("[{\"action\":\"Created\"}]").Should().Be(LogFormat.Json);
    }

    [Fact]
    public void DetectFormatType_WithNull_ShouldDefaultToXml()
    {
        // Act & Assert
        ChangeLogService.DetectFormatType(null).Should().Be(LogFormat.Xml);
        ChangeLogService.DetectFormatType("").Should().Be(LogFormat.Xml);
        ChangeLogService.DetectFormatType("   ").Should().Be(LogFormat.Xml);
    }

    [Fact]
    public void FullWorkflow_WithXml()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Xml };
        var service = new ChangeLogService(options);
        var user = "test@example.com";

        // Act - Full workflow
        var log = service.CreateEntry(null, user);
        log = service.UpdateEntry(log,
        [
            new() { Name = "Price", Before = "100", After = "150" }
        ], user);
        log = service.RelatedEntry(log, AuditAction.Added, "Tag",
        [
            new() { Name = "Name", Value = "Sale" }
        ], user);
        log = service.DeleteEntry(log, user);

        var entries = service.ParseAuditLog(log);

        // Assert
        entries.Should().HaveCount(4);
        entries[0].Action.Should().Be(AuditAction.Created);
        entries[1].Action.Should().Be(AuditAction.Updated);
        entries[2].Action.Should().Be(AuditAction.Added);
        entries[2].Related.Should().Be("Tag");
        entries[3].Action.Should().Be(AuditAction.Deleted);
    }

    [Fact]
    public void FullWorkflow_WithJson()
    {
        // Arrange
        var options = new AuditaXOptions { LogFormat = LogFormat.Json };
        var service = new ChangeLogService(options);
        var user = "test@example.com";

        // Act - Full workflow
        var log = service.CreateEntry(null, user);
        log = service.UpdateEntry(log,
        [
            new() { Name = "Price", Before = "100", After = "150" }
        ], user);
        log = service.RelatedEntry(log, AuditAction.Added, "Tag",
        [
            new() { Name = "Name", Value = "Sale" }
        ], user);
        log = service.DeleteEntry(log, user);

        var entries = service.ParseAuditLog(log);

        // Assert
        entries.Should().HaveCount(4);
        entries[0].Action.Should().Be(AuditAction.Created);
        entries[1].Action.Should().Be(AuditAction.Updated);
        entries[2].Action.Should().Be(AuditAction.Added);
        entries[2].Related.Should().Be("Tag");
        entries[3].Action.Should().Be(AuditAction.Deleted);
    }
}
