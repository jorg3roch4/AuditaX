using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;
using Moq;

namespace AuditaX.Tests.Interfaces;

/// <summary>
/// Tests for IAuditQueryService pagination functionality.
/// These tests verify the interface contract and default parameter values.
/// </summary>
public class IAuditQueryServicePaginationTests
{
    #region GetBySourceNameAsync Default Parameter Tests

    [Fact]
    public void GetBySourceNameAsync_ShouldHaveDefaultSkipOfZero()
    {
        // Arrange
        var mockService = new Mock<IAuditQueryService>();
        mockService
            .Setup(s => s.GetBySourceNameAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<AuditQueryResult>());

        // Act - Call without skip parameter
        mockService.Object.GetBySourceNameAsync("Product");

        // Assert - Verify default skip is 0
        mockService.Verify(s => s.GetBySourceNameAsync(
            "Product",
            0, // default skip
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetBySourceNameAsync_ShouldHaveDefaultTakeOf100()
    {
        // Arrange
        var mockService = new Mock<IAuditQueryService>();
        mockService
            .Setup(s => s.GetBySourceNameAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<AuditQueryResult>());

        // Act - Call without take parameter
        mockService.Object.GetBySourceNameAsync("Product");

        // Assert - Verify default take is 100
        mockService.Verify(s => s.GetBySourceNameAsync(
            "Product",
            It.IsAny<int>(),
            100, // default take
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBySourceNameAsync_WithExplicitPagination_ShouldPassCorrectValues()
    {
        // Arrange
        List<AuditQueryResult> expectedResults =
        [
            new() { SourceName = "Product", SourceKey = "1", AuditLog = "{}" },
            new() { SourceName = "Product", SourceKey = "2", AuditLog = "{}" }
        ];

        var mockService = new Mock<IAuditQueryService>();
        mockService
            .Setup(s => s.GetBySourceNameAsync("Product", 10, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        // Act
        var results = await mockService.Object.GetBySourceNameAsync("Product", skip: 10, take: 20);

        // Assert
        results.Should().HaveCount(2);
        mockService.Verify(s => s.GetBySourceNameAsync("Product", 10, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetBySourceNameAndDateAsync Default Parameter Tests

    [Fact]
    public void GetBySourceNameAndDateAsync_ShouldHaveDefaultSkipOfZero()
    {
        // Arrange
        var mockService = new Mock<IAuditQueryService>();
        var fromDate = DateTime.UtcNow.AddDays(-7);

        mockService
            .Setup(s => s.GetBySourceNameAndDateAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<AuditQueryResult>());

        // Act
        mockService.Object.GetBySourceNameAndDateAsync("Product", fromDate);

        // Assert
        mockService.Verify(s => s.GetBySourceNameAndDateAsync(
            "Product",
            fromDate,
            null,
            0, // default skip
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetBySourceNameAndDateAsync_ShouldHaveDefaultTakeOf100()
    {
        // Arrange
        var mockService = new Mock<IAuditQueryService>();
        var fromDate = DateTime.UtcNow.AddDays(-7);

        mockService
            .Setup(s => s.GetBySourceNameAndDateAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<AuditQueryResult>());

        // Act
        mockService.Object.GetBySourceNameAndDateAsync("Product", fromDate);

        // Assert
        mockService.Verify(s => s.GetBySourceNameAndDateAsync(
            "Product",
            fromDate,
            null,
            It.IsAny<int>(),
            100, // default take
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WithAllParameters_ShouldPassCorrectValues()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;
        List<AuditQueryResult> expectedResults =
        [
            new() { SourceName = "Product", SourceKey = "1", AuditLog = "{}" }
        ];

        var mockService = new Mock<IAuditQueryService>();
        mockService
            .Setup(s => s.GetBySourceNameAndDateAsync("Product", fromDate, toDate, 50, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        // Act
        var results = await mockService.Object.GetBySourceNameAndDateAsync(
            "Product", fromDate, toDate: toDate, skip: 50, take: 25);

        // Assert
        results.Should().HaveCount(1);
        mockService.Verify(s => s.GetBySourceNameAndDateAsync(
            "Product", fromDate, toDate, 50, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetSummaryBySourceNameAsync Default Parameter Tests

    [Fact]
    public void GetSummaryBySourceNameAsync_ShouldHaveDefaultSkipOfZero()
    {
        // Arrange
        var mockService = new Mock<IAuditQueryService>();
        mockService
            .Setup(s => s.GetSummaryBySourceNameAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<AuditSummaryResult>());

        // Act
        mockService.Object.GetSummaryBySourceNameAsync("Product");

        // Assert
        mockService.Verify(s => s.GetSummaryBySourceNameAsync(
            "Product",
            0, // default skip
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetSummaryBySourceNameAsync_ShouldHaveDefaultTakeOf100()
    {
        // Arrange
        var mockService = new Mock<IAuditQueryService>();
        mockService
            .Setup(s => s.GetSummaryBySourceNameAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<AuditSummaryResult>());

        // Act
        mockService.Object.GetSummaryBySourceNameAsync("Product");

        // Assert
        mockService.Verify(s => s.GetSummaryBySourceNameAsync(
            "Product",
            It.IsAny<int>(),
            100, // default take
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSummaryBySourceNameAsync_WithExplicitPagination_ShouldPassCorrectValues()
    {
        // Arrange
        List<AuditSummaryResult> expectedResults =
        [
            new()
            {
                SourceName = "Product",
                SourceKey = "1",
                LastAction = "Updated",
                LastTimestamp = DateTime.UtcNow,
                LastUser = "user@example.com"
            }
        ];

        var mockService = new Mock<IAuditQueryService>();
        mockService
            .Setup(s => s.GetSummaryBySourceNameAsync("Product", 5, 15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        // Act
        var results = await mockService.Object.GetSummaryBySourceNameAsync("Product", skip: 5, take: 15);

        // Assert
        results.Should().HaveCount(1);
        mockService.Verify(s => s.GetSummaryBySourceNameAsync("Product", 5, 15, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Non-Paginated Methods Tests

    [Fact]
    public async Task GetBySourceNameAndKeyAsync_ShouldNotHavePaginationParameters()
    {
        // Arrange
        var expectedResult = new AuditQueryResult
        {
            SourceName = "Product",
            SourceKey = "123",
            AuditLog = "{}"
        };

        var mockService = new Mock<IAuditQueryService>();
        mockService
            .Setup(s => s.GetBySourceNameAndKeyAsync("Product", "123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await mockService.Object.GetBySourceNameAndKeyAsync("Product", "123");

        // Assert
        result.Should().NotBeNull();
        result!.SourceKey.Should().Be("123");
    }

    [Fact]
    public async Task GetBySourceNameAndActionAsync_ShouldNotHavePaginationParameters()
    {
        // Arrange
        List<AuditQueryResult> expectedResults =
        [
            new() { SourceName = "Product", SourceKey = "1", AuditLog = "{}" }
        ];

        var mockService = new Mock<IAuditQueryService>();
        mockService
            .Setup(s => s.GetBySourceNameAndActionAsync("Product", AuditAction.Created, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        // Act
        var results = await mockService.Object.GetBySourceNameAndActionAsync("Product", AuditAction.Created);

        // Assert
        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetBySourceNameActionAndDateAsync_ShouldNotHavePaginationParameters()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-7);
        List<AuditQueryResult> expectedResults =
        [
            new() { SourceName = "Product", SourceKey = "1", AuditLog = "{}" }
        ];

        var mockService = new Mock<IAuditQueryService>();
        mockService
            .Setup(s => s.GetBySourceNameActionAndDateAsync("Product", AuditAction.Updated, fromDate, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        // Act
        var results = await mockService.Object.GetBySourceNameActionAndDateAsync("Product", AuditAction.Updated, fromDate);

        // Assert
        results.Should().HaveCount(1);
    }

    #endregion

    #region Pagination Behavior Tests

    [Theory]
    [InlineData(0, 10, 10)]
    [InlineData(0, 50, 50)]
    [InlineData(10, 10, 10)]
    public async Task GetBySourceNameAsync_WithPagination_ShouldReturnCorrectCount(int skip, int take, int expectedCount)
    {
        // Arrange
        var allResults = Enumerable.Range(1, 100)
            .Select(i => new AuditQueryResult { SourceName = "Product", SourceKey = i.ToString(), AuditLog = "{}" })
            .ToList();

        var paginatedResults = allResults.Skip(skip).Take(take).ToList();

        var mockService = new Mock<IAuditQueryService>();
        mockService
            .Setup(s => s.GetBySourceNameAsync("Product", skip, take, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResults);

        // Act
        var results = await mockService.Object.GetBySourceNameAsync("Product", skip: skip, take: take);

        // Assert
        results.Should().HaveCount(expectedCount);
    }

    [Fact]
    public async Task GetBySourceNameAsync_Pagination_ShouldSupportIteratingThroughPages()
    {
        // Arrange
        var allResults = Enumerable.Range(1, 25)
            .Select(i => new AuditQueryResult { SourceName = "Product", SourceKey = i.ToString(), AuditLog = "{}" })
            .ToList();

        var mockService = new Mock<IAuditQueryService>();

        // Setup for page 1 (skip=0, take=10)
        mockService
            .Setup(s => s.GetBySourceNameAsync("Product", 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allResults.Skip(0).Take(10).ToList());

        // Setup for page 2 (skip=10, take=10)
        mockService
            .Setup(s => s.GetBySourceNameAsync("Product", 10, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allResults.Skip(10).Take(10).ToList());

        // Setup for page 3 (skip=20, take=10)
        mockService
            .Setup(s => s.GetBySourceNameAsync("Product", 20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allResults.Skip(20).Take(10).ToList());

        // Act
        var page1 = await mockService.Object.GetBySourceNameAsync("Product", skip: 0, take: 10);
        var page2 = await mockService.Object.GetBySourceNameAsync("Product", skip: 10, take: 10);
        var page3 = await mockService.Object.GetBySourceNameAsync("Product", skip: 20, take: 10);

        // Assert
        page1.Should().HaveCount(10);
        page2.Should().HaveCount(10);
        page3.Should().HaveCount(5); // Only 5 remaining
    }

    #endregion
}
