using AuditaX.Enums;
using AuditaX.Models;
using AuditaX.Services;

namespace AuditaX.Tests.Services;

public class XmlChangeLogServiceTests
{
    private readonly XmlChangeLogService _service;

    public XmlChangeLogServiceTests()
    {
        _service = new XmlChangeLogService();
    }

    [Fact]
    public void CreateEntry_ShouldGenerateValidXml()
    {
        // Arrange
        var user = "test@example.com";

        // Act
        var result = _service.CreateEntry(null, user);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("<AuditLog>");
        result.Should().Contain("Action=\"Created\"");
        result.Should().Contain($"User=\"{user}\"");
        result.Should().Contain("Timestamp=");
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
        result.Should().Contain("Action=\"Updated\"");
        result.Should().Contain("Name=\"Price\"");
        result.Should().Contain("Before=\"100\"");
        result.Should().Contain("After=\"150\"");
        result.Should().Contain("Name=\"Stock\"");
    }

    [Fact]
    public void DeleteEntry_ShouldGenerateDeleteAction()
    {
        // Arrange
        var user = "admin@example.com";

        // Act
        var result = _service.DeleteEntry(null, user);

        // Assert
        result.Should().Contain("Action=\"Deleted\"");
        result.Should().Contain($"User=\"{user}\"");
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
        result.Should().Contain("Action=\"Added\"");
        result.Should().Contain("Related=\"ProductTag\"");
        result.Should().Contain("Name=\"Tag\"");
        result.Should().Contain("Value=\"Electronics\"");
    }

    [Fact]
    public void ParseAuditLog_ShouldParseValidXml()
    {
        // Arrange
        var user = "test@example.com";
        List<FieldChange> changes =
        [
            new() { Name = "Price", Before = "100", After = "150" }
        ];

        var xml = _service.CreateEntry(null, user);
        xml = _service.UpdateEntry(xml, changes, user);

        // Act
        var entries = _service.ParseAuditLog(xml);

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
    public void ParseAuditLog_WithInvalidXml_ShouldReturnEmptyList()
    {
        // Act
        var result = _service.ParseAuditLog("not valid xml");

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
    public void ConvertToString_ShouldHandleVariousTypes()
    {
        // Arrange & Act & Assert
        _service.ConvertToString(null).Should().BeNull();
        _service.ConvertToString("text").Should().Be("text");
        _service.ConvertToString(123).Should().Be("123");
        _service.ConvertToString(true).Should().Be("true");
        _service.ConvertToString(false).Should().Be("false");
        _service.ConvertToString(123.45m).Should().Be("123.45");
    }

    [Fact]
    public void ConvertToString_DateTime_ShouldUseIso8601Format()
    {
        // Arrange
        var dateTime = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var result = _service.ConvertToString(dateTime);

        // Assert
        result.Should().Contain("2025-01-15");
        result.Should().Contain("10:30:00");
    }
}
