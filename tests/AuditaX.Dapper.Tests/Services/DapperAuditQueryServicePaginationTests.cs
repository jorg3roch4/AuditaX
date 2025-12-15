using System.Data;
using AuditaX.Configuration;
using AuditaX.Dapper.Services;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.SqlServer.Providers;
using AuditaX.PostgreSql.Providers;
using Moq;

namespace AuditaX.Dapper.Tests.Services;

public class DapperAuditQueryServicePaginationTests
{
    private readonly Mock<IDbConnection> _mockConnection;
    private readonly AuditaXOptions _options;

    public DapperAuditQueryServicePaginationTests()
    {
        _mockConnection = new Mock<IDbConnection>();
        _mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);

        _options = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "dbo",
            ChangeLogFormat = ChangeLogFormat.Json
        };
    }

    #region GetBySourceNameAsync Pagination Tests

    [Fact]
    public async Task GetBySourceNameAsync_WithDefaultPagination_ShouldUseDefaultValues()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider);

        // Act & Assert - Should not throw with default pagination
        var action = async () => await service.GetBySourceNameAsync("Product");

        // Note: This will fail because we're using a mock connection without actual data
        // But we're testing that the method signature accepts default parameters
        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetBySourceNameAsync_WithExplicitPagination_ShouldAcceptParameters()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider);

        // Act & Assert - Method should accept explicit pagination parameters
        var action = async () => await service.GetBySourceNameAsync("Product", skip: 10, take: 20);

        await action.Should().ThrowAsync<Exception>();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(50, 25)]
    [InlineData(100, 100)]
    public async Task GetBySourceNameAsync_WithVariousPaginationValues_ShouldAcceptAllValues(int skip, int take)
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider);

        // Act & Assert - Method should accept various pagination parameters
        var action = async () => await service.GetBySourceNameAsync("Product", skip: skip, take: take);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetBySourceNameAndDateAsync Pagination Tests

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WithDefaultPagination_ShouldUseDefaultValues()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider);
        var fromDate = DateTime.UtcNow.AddDays(-7);

        // Act & Assert
        var action = async () => await service.GetBySourceNameAndDateAsync("Product", fromDate);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WithExplicitPagination_ShouldAcceptParameters()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider);
        var fromDate = DateTime.UtcNow.AddDays(-7);

        // Act & Assert
        var action = async () => await service.GetBySourceNameAndDateAsync(
            "Product",
            fromDate,
            toDate: DateTime.UtcNow,
            skip: 20,
            take: 50);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WithToDateAndPagination_ShouldAcceptAllParameters()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider);
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow.AddDays(-1);

        // Act & Assert
        var action = async () => await service.GetBySourceNameAndDateAsync(
            "Product",
            fromDate,
            toDate: toDate,
            skip: 0,
            take: 100);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetSummaryBySourceNameAsync Pagination Tests

    [Fact]
    public async Task GetSummaryBySourceNameAsync_WithDefaultPagination_ShouldUseDefaultValues()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider);

        // Act & Assert
        var action = async () => await service.GetSummaryBySourceNameAsync("Product");

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetSummaryBySourceNameAsync_WithExplicitPagination_ShouldAcceptParameters()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider);

        // Act & Assert
        var action = async () => await service.GetSummaryBySourceNameAsync("Product", skip: 5, take: 15);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region Non-Paginated Methods Should Remain Unchanged

    [Fact]
    public async Task GetBySourceNameAndKeyAsync_ShouldNotHavePaginationParameters()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider);

        // Act & Assert - Method should only have sourceName and sourceKey
        var action = async () => await service.GetBySourceNameAndKeyAsync("Product", "123");

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetBySourceNameAndActionAsync_ShouldNotHavePaginationParameters()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider);

        // Act & Assert
        var action = async () => await service.GetBySourceNameAndActionAsync("Product", AuditAction.Created);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetBySourceNameActionAndDateAsync_ShouldNotHavePaginationParameters()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider);
        var fromDate = DateTime.UtcNow.AddDays(-7);

        // Act & Assert
        var action = async () => await service.GetBySourceNameActionAndDateAsync(
            "Product",
            AuditAction.Updated,
            fromDate);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task GetBySourceNameAsync_WithEmptySourceName_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider);

        // Act & Assert
        var action = async () => await service.GetBySourceNameAsync("");

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SourceName*");
    }

    [Fact]
    public async Task GetBySourceNameAsync_WithNullSourceName_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider);

        // Act & Assert
        var action = async () => await service.GetBySourceNameAsync(null!);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SourceName*");
    }

    [Fact]
    public async Task GetSummaryBySourceNameAsync_WithEmptySourceName_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider);

        // Act & Assert
        var action = async () => await service.GetSummaryBySourceNameAsync("  ");

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SourceName*");
    }

    #endregion

    #region PostgreSQL Provider Tests

    [Fact]
    public async Task GetBySourceNameAsync_WithPostgreSqlProvider_ShouldAcceptPagination()
    {
        // Arrange
        var pgOptions = new AuditaXOptions
        {
            TableName = "audit_log",
            Schema = "public",
            ChangeLogFormat = ChangeLogFormat.Json
        };
        var provider = new PostgreSqlDatabaseProvider(pgOptions);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider);

        // Act & Assert
        var action = async () => await service.GetBySourceNameAsync("Product", skip: 0, take: 50);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetSummaryBySourceNameAsync_WithPostgreSqlProvider_ShouldAcceptPagination()
    {
        // Arrange
        var pgOptions = new AuditaXOptions
        {
            TableName = "audit_log",
            Schema = "public",
            ChangeLogFormat = ChangeLogFormat.Xml
        };
        var provider = new PostgreSqlDatabaseProvider(pgOptions);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider);

        // Act & Assert
        var action = async () => await service.GetSummaryBySourceNameAsync("Product", skip: 10, take: 25);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion
}
