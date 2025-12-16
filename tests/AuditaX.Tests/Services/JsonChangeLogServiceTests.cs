using AuditaX.Enums;
using AuditaX.Models;
using AuditaX.Services;

namespace AuditaX.Tests.Services;

public class JsonChangeLogServiceTests
{
    private readonly JsonChangeLogService _service;

    public JsonChangeLogServiceTests()
    {
        _service = new JsonChangeLogService();
    }

    [Fact]
    public void CreateEntry_ShouldGenerateValidJson()
    {
        // Arrange
        var user = "test@example.com";

        // Act
        var result = _service.CreateEntry(null, user);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("\"auditLog\"");
        result.Should().Contain("\"action\":\"Created\"");
        result.Should().Contain($"\"user\":\"{user}\"");
        result.Should().Contain("\"timestamp\"");
    }

    [Fact]
    public void CreateEntry_WithExistingLog_ShouldAppend()
    {
        // Arrange
        var user = "test@example.com";
        var existing = _service.CreateEntry(null, "first@example.com");

        // Act
        var result = _service.CreateEntry(existing, user);

        // Assert
        result.Should().Contain("first@example.com");
        result.Should().Contain(user);
    }

    [Fact]
    public void UpdateEntry_ShouldIncludeFieldChanges()
    {
        // Arrange
        var user = "test@example.com";
        List<FieldChange> changes =
        [
            new() { Name = "Price", Before = "100", After = "150" },
            new() { Name = "Stock", Before = "10", After = "5" }
        ];

        // Act
        var result = _service.UpdateEntry(null, changes, user);

        // Assert
        result.Should().Contain("\"action\":\"Updated\"");
        result.Should().Contain("\"name\":\"Price\"");
        result.Should().Contain("\"before\":\"100\"");
        result.Should().Contain("\"after\":\"150\"");
        result.Should().Contain("\"name\":\"Stock\"");
    }

    [Fact]
    public void DeleteEntry_ShouldGenerateDeleteAction()
    {
        // Arrange
        var user = "admin@example.com";

        // Act
        var result = _service.DeleteEntry(null, user);

        // Assert
        result.Should().Contain("\"action\":\"Deleted\"");
        result.Should().Contain($"\"user\":\"{user}\"");
    }

    [Fact]
    public void RelatedEntry_ShouldIncludeRelatedName()
    {
        // Arrange
        var user = "test@example.com";
        List<FieldChange> fields =
        [
            new() { Name = "Tag", Value = "Electronics" }
        ];

        // Act
        var result = _service.RelatedEntry(null, AuditAction.Added, "ProductTag", fields, user);

        // Assert
        result.Should().Contain("\"action\":\"Added\"");
        result.Should().Contain("\"related\":\"ProductTag\"");
        result.Should().Contain("\"name\":\"Tag\"");
        result.Should().Contain("\"value\":\"Electronics\"");
    }

    [Fact]
    public void ParseAuditLog_ShouldParseValidJson()
    {
        // Arrange
        var user = "test@example.com";
        List<FieldChange> changes =
        [
            new() { Name = "Price", Before = "100", After = "150" }
        ];

        var json = _service.CreateEntry(null, user);
        json = _service.UpdateEntry(json, changes, user);

        // Act
        var entries = _service.ParseAuditLog(json);

        // Assert
        entries.Should().HaveCount(2);
        entries[0].Action.Should().Be(AuditAction.Created);
        entries[0].User.Should().Be(user);
        entries[1].Action.Should().Be(AuditAction.Updated);
        entries[1].Fields.Should().HaveCount(1);
        entries[1].Fields[0].Name.Should().Be("Price");
    }

    [Fact]
    public void ParseAuditLog_WithNullInput_ShouldReturnEmptyList()
    {
        // Act
        var result = _service.ParseAuditLog(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseAuditLog_WithInvalidJson_ShouldReturnEmptyList()
    {
        // Act
        var result = _service.ParseAuditLog("not valid json");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void HasChanged_WithDifferentValues_ShouldReturnTrue()
    {
        // Act & Assert
        _service.HasChanged("old", "new").Should().BeTrue();
        _service.HasChanged(100, 200).Should().BeTrue();
        _service.HasChanged(true, false).Should().BeTrue();
    }

    [Fact]
    public void HasChanged_WithSameValues_ShouldReturnFalse()
    {
        // Act & Assert
        _service.HasChanged("same", "same").Should().BeFalse();
        _service.HasChanged(100, 100).Should().BeFalse();
        _service.HasChanged(null, null).Should().BeFalse();
    }

    [Fact]
    public void JsonOutput_ShouldBeCompact()
    {
        // Arrange
        var user = "test@example.com";

        // Act
        var result = _service.CreateEntry(null, user);

        // Assert - JSON should not have unnecessary whitespace
        result.Should().NotContain("\n");
        result.Should().NotContain("  "); // No indentation
    }

    [Fact]
    public void JsonOutput_ShouldUseCamelCase()
    {
        // Arrange
        var user = "test@example.com";

        // Act
        var result = _service.CreateEntry(null, user);

        // Assert
        result.Should().Contain("\"auditLog\"");
        result.Should().Contain("\"action\"");
        result.Should().Contain("\"user\"");
        result.Should().Contain("\"timestamp\"");
        result.Should().NotContain("\"AuditLog\""); // Should be camelCase, not PascalCase
        result.Should().NotContain("\"Action\"");
    }
}
