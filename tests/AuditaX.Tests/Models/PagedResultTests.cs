using System.Linq;
using AuditaX.Models;

namespace AuditaX.Tests.Models;

public class PagedResultTests
{
    [Fact]
    public void PagedResult_DefaultValues_ShouldHaveEmptyItemsAndZeroCount()
    {
        // Act
        var result = new PagedResult<string>();

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public void PagedResult_WithItems_ShouldRetainItems()
    {
        // Arrange
        var items = new[] { "a", "b", "c" };

        // Act
        var result = new PagedResult<string> { Items = items, TotalCount = 10 };

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
    }

    [Fact]
    public void PagedResult_WithAuditQueryResult_ShouldWorkWithDomainModels()
    {
        // Arrange
        var items = new List<AuditQueryResult>
        {
            new() { SourceName = "Product", SourceKey = "1", AuditLog = "<xml/>" },
            new() { SourceName = "Product", SourceKey = "2", AuditLog = "<xml/>" }
        };

        // Act
        var result = new PagedResult<AuditQueryResult> { Items = items, TotalCount = 50 };

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(50);
        result.Items.First().SourceName.Should().Be("Product");
    }

    [Fact]
    public void PagedResult_WithAuditSummaryResult_ShouldWorkWithSummaryModels()
    {
        // Arrange
        var items = new List<AuditSummaryResult>
        {
            new() { SourceName = "Product", SourceKey = "1", LastAction = "Created", LastUser = "admin" }
        };

        // Act
        var result = new PagedResult<AuditSummaryResult> { Items = items, TotalCount = 1 };

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public void PagedResult_IsRecord_ShouldSupportValueEquality()
    {
        // Arrange
        var items = Array.Empty<string>();
        var result1 = new PagedResult<string> { Items = items, TotalCount = 5 };
        var result2 = new PagedResult<string> { Items = items, TotalCount = 5 };

        // Assert
        result1.Should().Be(result2);
    }
}
