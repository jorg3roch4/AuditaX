using AuditaX.Enums;
using AuditaX.Models;

namespace AuditaX.Tests.Models;

public class AuditLogEntryTests
{
    [Fact]
    public void DefaultValues_ShouldHaveDefaults()
    {
        var entry = new AuditLogEntry();

        entry.Action.Should().Be(AuditAction.Created); // default enum value
        entry.User.Should().Be(string.Empty);
        entry.Timestamp.Should().Be(default(DateTime));
        entry.Related.Should().BeNull();
        entry.Fields.Should().BeEmpty();
    }

    [Fact]
    public void WithAllProperties_ShouldRetainValues()
    {
        var timestamp = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var entry = new AuditLogEntry
        {
            Action = AuditAction.Updated,
            User = "admin",
            Timestamp = timestamp,
            Related = "Tags",
            Fields =
            [
                new FieldChange { Name = "Price", Before = "10", After = "20" }
            ]
        };

        entry.Action.Should().Be(AuditAction.Updated);
        entry.User.Should().Be("admin");
        entry.Timestamp.Should().Be(timestamp);
        entry.Related.Should().Be("Tags");
        entry.Fields.Should().HaveCount(1);
    }

    [Fact]
    public void Properties_ShouldBeMutable()
    {
        var entry = new AuditLogEntry();

        entry.Action = AuditAction.Deleted;
        entry.User = "system";
        entry.Related = "Comments";

        entry.Action.Should().Be(AuditAction.Deleted);
        entry.User.Should().Be("system");
        entry.Related.Should().Be("Comments");
    }

    [Fact]
    public void Fields_ShouldBeInitializedAsList()
    {
        var entry = new AuditLogEntry();

        entry.Fields.Add(new FieldChange { Name = "Name", Value = "Test" });

        entry.Fields.Should().HaveCount(1);
        entry.Fields[0].Name.Should().Be("Name");
    }

    [Fact]
    public void Related_ShouldBeNullable()
    {
        var entry = new AuditLogEntry { Related = "Tags" };

        entry.Related.Should().Be("Tags");

        entry.Related = null;
        entry.Related.Should().BeNull();
    }
}
