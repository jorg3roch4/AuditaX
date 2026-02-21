using AuditaX.Entities;

namespace AuditaX.Tests.Models;

public class AuditLogEntityTests
{
    [Fact]
    public void DefaultValues_ShouldHaveNewGuidAndEmptyStrings()
    {
        var log = new AuditLog();

        log.LogId.Should().NotBe(Guid.Empty);
        log.SourceName.Should().Be(string.Empty);
        log.SourceKey.Should().Be(string.Empty);
        log.AuditLogXml.Should().Be(string.Empty);
    }

    [Fact]
    public void LogId_ShouldGenerateUniqueGuids()
    {
        var log1 = new AuditLog();
        var log2 = new AuditLog();

        log1.LogId.Should().NotBe(log2.LogId);
    }

    [Fact]
    public void Properties_ShouldBeMutable()
    {
        var log = new AuditLog();
        var guid = Guid.NewGuid();

        log.LogId = guid;
        log.SourceName = "Product";
        log.SourceKey = "42";
        log.AuditLogXml = "<AuditLog />";

        log.LogId.Should().Be(guid);
        log.SourceName.Should().Be("Product");
        log.SourceKey.Should().Be("42");
        log.AuditLogXml.Should().Be("<AuditLog />");
    }

    [Fact]
    public void WithAllProperties_ShouldRetainValues()
    {
        var guid = Guid.NewGuid();
        var log = new AuditLog
        {
            LogId = guid,
            SourceName = "Order",
            SourceKey = "123",
            AuditLogXml = "{\"entries\":[]}"
        };

        log.LogId.Should().Be(guid);
        log.SourceName.Should().Be("Order");
        log.SourceKey.Should().Be("123");
        log.AuditLogXml.Should().Be("{\"entries\":[]}");
    }

    [Fact]
    public void IsClass_ShouldSupportReferenceEquality()
    {
        var log1 = new AuditLog { SourceName = "Product", SourceKey = "1" };
        var log2 = log1;

        log2.Should().BeSameAs(log1);
    }
}
