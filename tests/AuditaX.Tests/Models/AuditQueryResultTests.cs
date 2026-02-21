using AuditaX.Models;

namespace AuditaX.Tests.Models;

public class AuditQueryResultTests
{
    [Fact]
    public void DefaultValues_ShouldHaveEmptyStrings()
    {
        var result = new AuditQueryResult();

        result.SourceName.Should().Be(string.Empty);
        result.SourceKey.Should().Be(string.Empty);
        result.AuditLog.Should().Be(string.Empty);
    }

    [Fact]
    public void WithAllProperties_ShouldRetainValues()
    {
        var result = new AuditQueryResult
        {
            SourceName = "Product",
            SourceKey = "42",
            AuditLog = "<AuditLog><Entry /></AuditLog>"
        };

        result.SourceName.Should().Be("Product");
        result.SourceKey.Should().Be("42");
        result.AuditLog.Should().Be("<AuditLog><Entry /></AuditLog>");
    }

    [Fact]
    public void IsRecord_ShouldSupportValueEquality()
    {
        var result1 = new AuditQueryResult { SourceName = "Product", SourceKey = "1", AuditLog = "<xml/>" };
        var result2 = new AuditQueryResult { SourceName = "Product", SourceKey = "1", AuditLog = "<xml/>" };

        result1.Should().Be(result2);
    }

    [Fact]
    public void IsRecord_DifferentValues_ShouldNotBeEqual()
    {
        var result1 = new AuditQueryResult { SourceName = "Product", SourceKey = "1" };
        var result2 = new AuditQueryResult { SourceName = "Product", SourceKey = "2" };

        result1.Should().NotBe(result2);
    }
}
