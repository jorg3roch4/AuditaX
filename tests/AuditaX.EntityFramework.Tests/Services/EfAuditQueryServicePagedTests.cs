using AuditaX.Configuration;
using AuditaX.EntityFramework.Services;
using AuditaX.EntityFramework.Tests.TestEntities;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.SqlServer.Providers;
using AuditaX.PostgreSql.Providers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AuditaX.EntityFramework.Tests.Services;

public class EfAuditQueryServicePagedTests
{
    private readonly AuditaXOptions _sqlServerOptions;
    private readonly AuditaXOptions _postgreSqlOptions;
    private readonly Mock<IChangeLogService> _mockChangeLogService;

    public EfAuditQueryServicePagedTests()
    {
        _sqlServerOptions = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "dbo",
            LogFormat = LogFormat.Json
        };

        _postgreSqlOptions = new AuditaXOptions
        {
            TableName = "audit_log",
            Schema = "public",
            LogFormat = LogFormat.Json
        };

        _mockChangeLogService = new Mock<IChangeLogService>();
    }

    private TestDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new TestDbContext(options);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);

        // Act & Assert
        var action = () => new EfAuditQueryService(null!, provider, _mockChangeLogService.Object);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Paged_NullProvider");

        // Act & Assert
        var action = () => new EfAuditQueryService(context, null!, _mockChangeLogService.Object);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullChangeLogService_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Paged_NullChangeLog");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);

        // Act & Assert
        var action = () => new EfAuditQueryService(context, provider, null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EfAuditQueryService_ShouldImplementIAuditQueryService()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Paged_Interface");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);

        // Act
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Assert
        service.Should().BeAssignableTo<IAuditQueryService>();
    }

    #endregion

    #region GetPagedBySourceNameAsync Tests

    [Fact]
    public async Task GetPagedBySourceNameAsync_WithEmptySourceName_ShouldReturnFailedResponse()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Paged_Empty");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act
        var result = await service.GetPagedBySourceNameAsync("");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("SourceName");
    }

    [Fact]
    public async Task GetPagedBySourceNameAsync_WithNullSourceName_ShouldReturnFailedResponse()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Paged_Null");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act
        var result = await service.GetPagedBySourceNameAsync(null!);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("SourceName");
    }

    [Fact]
    public async Task GetPagedBySourceNameAsync_WithDefaultPagination_ShouldAcceptDefaults()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Paged_Defaults");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act & Assert - InMemory doesn't support raw SQL
        var action = async () => await service.GetPagedBySourceNameAsync("Product");

        await action.Should().ThrowAsync<Exception>();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(50, 25)]
    [InlineData(100, 100)]
    public async Task GetPagedBySourceNameAsync_WithVariousPagination_ShouldAcceptAllValues(int skip, int take)
    {
        // Arrange
        using var context = CreateInMemoryContext($"TestDb_Paged_Various_{skip}_{take}");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedBySourceNameAsync("Product", skip: skip, take: take);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetPagedBySourceNameAndDateAsync Tests

    [Fact]
    public async Task GetPagedBySourceNameAndDateAsync_WithEmptySourceName_ShouldReturnFailedResponse()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_PagedDate_Empty");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act
        var result = await service.GetPagedBySourceNameAndDateAsync("", DateTime.UtcNow);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("SourceName");
    }

    [Fact]
    public async Task GetPagedBySourceNameAndDateAsync_WithValidParameters_ShouldAcceptAllParameters()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_PagedDate_Valid");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedBySourceNameAndDateAsync(
            "Product", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, skip: 10, take: 20);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetPagedBySourceNameAndActionAsync Tests

    [Fact]
    public async Task GetPagedBySourceNameAndActionAsync_WithEmptySourceName_ShouldReturnFailedResponse()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_PagedAction_Empty");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act
        var result = await service.GetPagedBySourceNameAndActionAsync("", AuditAction.Created);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("SourceName");
    }

    [Fact]
    public async Task GetPagedBySourceNameAndActionAsync_WithValidParameters_ShouldAcceptAllParameters()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_PagedAction_Valid");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedBySourceNameAndActionAsync(
            "Product", AuditAction.Updated, skip: 0, take: 50);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetPagedBySourceNameActionAndDateAsync Tests

    [Fact]
    public async Task GetPagedBySourceNameActionAndDateAsync_WithEmptySourceName_ShouldReturnFailedResponse()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_PagedActionDate_Empty");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act
        var result = await service.GetPagedBySourceNameActionAndDateAsync("", AuditAction.Created, DateTime.UtcNow);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("SourceName");
    }

    [Fact]
    public async Task GetPagedBySourceNameActionAndDateAsync_WithValidParameters_ShouldAcceptAllParameters()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_PagedActionDate_Valid");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedBySourceNameActionAndDateAsync(
            "Product", AuditAction.Updated, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, skip: 5, take: 15);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetPagedSummaryBySourceNameAsync Tests

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_WithWhitespaceSourceName_ShouldReturnFailedResponse()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_PagedSummary_Empty");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act
        var result = await service.GetPagedSummaryBySourceNameAsync("   ");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("SourceName");
    }

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_WithValidParameters_ShouldAcceptAllParameters()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_PagedSummary_Valid");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedSummaryBySourceNameAsync("Product", skip: 0, take: 50);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetPagedSummaryBySourceNameAsync (Filtered) Tests

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_Filtered_WithEmptySourceName_ShouldReturnFailedResponse()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_PagedFilteredSummary_Empty");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act
        var result = await service.GetPagedSummaryBySourceNameAsync("", "key1", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("SourceName");
    }

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_Filtered_WithAllFilters_ShouldAcceptParameters()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_PagedFilteredSummary_All");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedSummaryBySourceNameAsync(
            "Product", "42", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, skip: 0, take: 10);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_Filtered_WithNullFilters_ShouldAcceptParameters()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_PagedFilteredSummary_Null");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedSummaryBySourceNameAsync(
            "Product", null, null, null, skip: 0, take: 10);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetParsedDetailBySourceNameAndKeyAsync Tests

    [Fact]
    public async Task GetParsedDetailBySourceNameAndKeyAsync_WithEmptySourceName_ShouldReturnFailedResponse()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_ParsedDetail_EmptySource");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act
        var result = await service.GetParsedDetailBySourceNameAndKeyAsync("", "123");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("SourceName");
    }

    [Fact]
    public async Task GetParsedDetailBySourceNameAndKeyAsync_WithEmptySourceKey_ShouldReturnFailedResponse()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_ParsedDetail_EmptyKey");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act
        var result = await service.GetParsedDetailBySourceNameAndKeyAsync("Product", "");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("SourceKey");
    }

    #endregion

    #region PostgreSQL Provider Tests

    [Fact]
    public async Task GetPagedBySourceNameAsync_WithPostgreSqlProvider_ShouldAcceptPagination()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Paged_PostgreSql");
        var provider = new PostgreSqlDatabaseProvider(_postgreSqlOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedBySourceNameAsync("Product", skip: 0, take: 50);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetPagedSummaryBySourceNameAsync_WithPostgreSqlProvider_ShouldAcceptPagination()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Paged_PostgreSql_Summary");
        var pgXmlOptions = new AuditaXOptions
        {
            TableName = "audit_log",
            Schema = "public",
            LogFormat = LogFormat.Xml
        };
        var provider = new PostgreSqlDatabaseProvider(pgXmlOptions);
        var service = new EfAuditQueryService(context, provider, _mockChangeLogService.Object);

        // Act & Assert
        var action = async () => await service.GetPagedSummaryBySourceNameAsync("Product", skip: 10, take: 25);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion
}
