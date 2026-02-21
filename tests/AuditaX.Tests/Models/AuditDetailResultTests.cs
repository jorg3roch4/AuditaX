using AuditaX.Enums;
using AuditaX.Models;

namespace AuditaX.Tests.Models;

public class AuditDetailResultTests
{
    [Fact]
    public void AuditDetailResult_DefaultValues_ShouldHaveDefaults()
    {
        // Act
        var result = new AuditDetailResult();

        // Assert
        result.SourceName.Should().Be(string.Empty);
        result.SourceKey.Should().Be(string.Empty);
        result.Entries.Should().BeEmpty();
    }

    [Fact]
    public void AuditDetailResult_WithEntries_ShouldRetainAllData()
    {
        // Arrange
        var entries = new List<AuditLogEntry>
        {
            new()
            {
                Action = AuditAction.Created,
                User = "admin",
                Timestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Fields =
                [
                    new FieldChange { Name = "Name", Value = "Widget" }
                ]
            },
            new()
            {
                Action = AuditAction.Updated,
                User = "editor",
                Timestamp = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                Fields =
                [
                    new FieldChange { Name = "Name", Before = "Widget", After = "Super Widget" }
                ]
            }
        };

        // Act
        var result = new AuditDetailResult
        {
            SourceName = "Product",
            SourceKey = "42",
            Entries = entries
        };

        // Assert
        result.SourceName.Should().Be("Product");
        result.SourceKey.Should().Be("42");
        result.Entries.Should().HaveCount(2);
        result.Entries[0].Action.Should().Be(AuditAction.Created);
        result.Entries[0].Fields[0].Value.Should().Be("Widget");
        result.Entries[1].Action.Should().Be(AuditAction.Updated);
        result.Entries[1].Fields[0].Before.Should().Be("Widget");
        result.Entries[1].Fields[0].After.Should().Be("Super Widget");
    }

    [Fact]
    public void AuditDetailResult_IsRecord_ShouldSupportValueEquality()
    {
        // Arrange
        var entries = new List<AuditLogEntry>();
        var result1 = new AuditDetailResult { SourceName = "Product", SourceKey = "1", Entries = entries };
        var result2 = new AuditDetailResult { SourceName = "Product", SourceKey = "1", Entries = entries };

        // Assert
        result1.Should().Be(result2);
    }
}
