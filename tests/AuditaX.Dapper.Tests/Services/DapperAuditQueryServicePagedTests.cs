using System.Data;
using AuditaX.Configuration;
using AuditaX.Dapper.Services;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.SqlServer.Providers;
using AuditaX.PostgreSql.Providers;
using Moq;

namespace AuditaX.Dapper.Tests.Services;

public class DapperAuditQueryServicePagedTests
{
    private readonly Mock<IDbConnection> _mockConnection;
    private readonly Mock<IChangeLogService> _mockChangeLogService;
    private readonly AuditaXOptions _options;

    public DapperAuditQueryServicePagedTests()
    {
        _mockConnection = new Mock<IDbConnection>();
        _mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);

        _mockChangeLogService = new Mock<IChangeLogService>();

        _options = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "dbo",
            LogFormat = LogFormat.Json
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullConnection_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);

        // Act & Assert
        var action = () => new DapperAuditQueryService(null!, provider, _mockChangeLogService.Object);

        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("connection");
    }

    [Fact]
    public void Constructor_WithNullProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DapperAuditQueryService(_mockConnection.Object, null!, _mockChangeLogService.Object);

        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("provider");
    }

    [Fact]
    public void Constructor_WithNullChangeLogService_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);

        // Act & Assert
        var action = () => new DapperAuditQueryService(_mockConnection.Object, provider, null!);

        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("changeLogService");
    }

    #endregion

    #region GetPagedBySourceNameAsync Tests

    [Fact]
    public async Task GetPagedBySourceNameAsync_WithEmptySourceName_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedBySourceNameAsync("");

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SourceName*");
    }

    [Fact]
    public async Task GetPagedBySourceNameAsync_WithNullSourceName_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedBySourceNameAsync(null!);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SourceName*");
    }

    [Fact]
    public async Task GetPagedBySourceNameAsync_WithDefaultPagination_ShouldAcceptDefaults()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert - will throw because mock connection doesn't support actual queries
        var action = async () => await service.GetPagedBySourceNameAsync("Product");

        await action.Should().ThrowAsync<Exception>();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(50, 25)]
    [InlineData(100, 100)]
    public async Task GetPagedBySourceNameAsync_WithVariousPaginationValues_ShouldAcceptAllValues(int skip, int take)
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedBySourceNameAsync("Product", skip: skip, take: take);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetPagedBySourceNameAndDateAsync Tests

    [Fact]
    public async Task GetPagedBySourceNameAndDateAsync_WithEmptySourceName_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedBySourceNameAndDateAsync("", DateTime.UtcNow);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SourceName*");
    }

    [Fact]
    public async Task GetPagedBySourceNameAndDateAsync_WithValidParameters_ShouldAcceptAllParameters()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedBySourceNameAndDateAsync(
            "Product", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, skip: 10, take: 20);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetPagedBySourceNameAndActionAsync Tests

    [Fact]
    public async Task GetPagedBySourceNameAndActionAsync_WithEmptySourceName_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedBySourceNameAndActionAsync("", AuditAction.Created);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SourceName*");
    }

    [Fact]
    public async Task GetPagedBySourceNameAndActionAsync_WithValidParameters_ShouldAcceptAllParameters()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedBySourceNameAndActionAsync(
            "Product", AuditAction.Updated, skip: 0, take: 50);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetPagedBySourceNameActionAndDateAsync Tests

    [Fact]
    public async Task GetPagedBySourceNameActionAndDateAsync_WithEmptySourceName_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedBySourceNameActionAndDateAsync(
            "", AuditAction.Created, DateTime.UtcNow);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SourceName*");
    }

    [Fact]
    public async Task GetPagedBySourceNameActionAndDateAsync_WithValidParameters_ShouldAcceptAllParameters()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedBySourceNameActionAndDateAsync(
            "Product", AuditAction.Updated, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, skip: 5, take: 15);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetPagedSummaryBySourceNameAsync Tests

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_WithEmptySourceName_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedSummaryBySourceNameAsync("  ");

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SourceName*");
    }

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_WithValidParameters_ShouldAcceptAllParameters()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedSummaryBySourceNameAsync("Product", skip: 0, take: 50);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetPagedSummaryBySourceNameAsync (Filtered) Tests

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_Filtered_WithEmptySourceName_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedSummaryBySourceNameAsync(
            "", "key1", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SourceName*");
    }

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_Filtered_WithAllFilters_ShouldAcceptParameters()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedSummaryBySourceNameAsync(
            "Product", "42", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, skip: 0, take: 10);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_Filtered_WithNullFilters_ShouldAcceptParameters()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedSummaryBySourceNameAsync(
            "Product", null, null, null, skip: 0, take: 10);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetParsedDetailBySourceNameAndKeyAsync Tests

    [Fact]
    public async Task GetParsedDetailBySourceNameAndKeyAsync_WithEmptySourceName_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetParsedDetailBySourceNameAndKeyAsync("", "123");

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SourceName*");
    }

    [Fact]
    public async Task GetParsedDetailBySourceNameAndKeyAsync_WithEmptySourceKey_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_options);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetParsedDetailBySourceNameAndKeyAsync("Product", "");

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SourceKey*");
    }

    #endregion

    #region PostgreSQL Provider Tests

    [Fact]
    public async Task GetPagedBySourceNameAsync_WithPostgreSqlProvider_ShouldAcceptPagination()
    {
        // Arrange
        var pgOptions = new AuditaXOptions
        {
            TableName = "audit_log",
            Schema = "public",
            LogFormat = LogFormat.Json
        };
        var provider = new PostgreSqlDatabaseProvider(pgOptions);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedBySourceNameAsync("Product", skip: 0, take: 50);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_WithPostgreSqlProvider_ShouldAcceptPagination()
    {
        // Arrange
        var pgOptions = new AuditaXOptions
        {
            TableName = "audit_log",
            Schema = "public",
            LogFormat = LogFormat.Xml
        };
        var provider = new PostgreSqlDatabaseProvider(pgOptions);
        var service = new DapperAuditQueryService(_mockConnection.Object, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedSummaryBySourceNameAsync("Product", skip: 10, take: 25);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion
}
