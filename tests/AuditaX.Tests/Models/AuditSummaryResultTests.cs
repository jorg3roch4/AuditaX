using AuditaX.Models;

namespace AuditaX.Tests.Models;

public class AuditSummaryResultTests
{
    [Fact]
    public void DefaultValues_ShouldHaveEmptyStringsAndDefaultDateTime()
    {
        var result = new AuditSummaryResult();

        result.SourceName.Should().Be(string.Empty);
        result.SourceKey.Should().Be(string.Empty);
        result.LastAction.Should().Be(string.Empty);
        result.LastTimestamp.Should().Be(default(DateTime));
        result.LastUser.Should().Be(string.Empty);
    }

    [Fact]
    public void WithAllProperties_ShouldRetainValues()
    {
        var timestamp = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var result = new AuditSummaryResult
        {
            SourceName = "Product",
            SourceKey = "42",
            LastAction = "Updated",
            LastTimestamp = timestamp,
            LastUser = "admin"
        };

        result.SourceName.Should().Be("Product");
        result.SourceKey.Should().Be("42");
        result.LastAction.Should().Be("Updated");
        result.LastTimestamp.Should().Be(timestamp);
        result.LastUser.Should().Be("admin");
    }

    [Fact]
    public void IsRecord_ShouldSupportValueEquality()
    {
        var timestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result1 = new AuditSummaryResult { SourceName = "Product", SourceKey = "1", LastTimestamp = timestamp };
        var result2 = new AuditSummaryResult { SourceName = "Product", SourceKey = "1", LastTimestamp = timestamp };

        result1.Should().Be(result2);
    }

    [Fact]
    public void IsRecord_DifferentTimestamp_ShouldNotBeEqual()
    {
        var result1 = new AuditSummaryResult { LastTimestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var result2 = new AuditSummaryResult { LastTimestamp = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc) };

        result1.Should().NotBe(result2);
    }

    [Fact]
    public void LastTimestamp_ShouldPreserveDateTimeValue()
    {
        var timestamp = new DateTime(2025, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);
        var result = new AuditSummaryResult { LastTimestamp = timestamp };

        result.LastTimestamp.Year.Should().Be(2025);
        result.LastTimestamp.Month.Should().Be(12);
        result.LastTimestamp.Day.Should().Be(31);
        result.LastTimestamp.Hour.Should().Be(23);
        result.LastTimestamp.Minute.Should().Be(59);
    }
}
