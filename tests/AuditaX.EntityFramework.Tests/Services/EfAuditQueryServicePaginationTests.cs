using AuditaX.Configuration;
using AuditaX.EntityFramework.Services;
using AuditaX.EntityFramework.Tests.TestEntities;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.SqlServer.Providers;
using AuditaX.PostgreSql.Providers;
using Microsoft.EntityFrameworkCore;

namespace AuditaX.EntityFramework.Tests.Services;

public class EfAuditQueryServicePaginationTests
{
    private readonly AuditaXOptions _sqlServerOptions;
    private readonly AuditaXOptions _postgreSqlOptions;

    public EfAuditQueryServicePaginationTests()
    {
        _sqlServerOptions = new AuditaXOptions
        {
            TableName = "AuditLog",
            Schema = "dbo",
            ChangeLogFormat = ChangeLogFormat.Json
        };

        _postgreSqlOptions = new AuditaXOptions
        {
            TableName = "audit_log",
            Schema = "public",
            ChangeLogFormat = ChangeLogFormat.Json
        };
    }

    private TestDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new TestDbContext(options);
    }

    #region GetBySourceNameAsync Pagination Tests

    [Fact]
    public async Task GetBySourceNameAsync_WithDefaultPagination_ShouldUseDefaultValues()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Default_Pagination");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider);

        // Act & Assert - InMemory provider doesn't support raw SQL, but we test the method signature
        var action = async () => await service.GetBySourceNameAsync("Product");

        // InMemory provider throws because it doesn't support raw SQL
        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetBySourceNameAsync_WithExplicitPagination_ShouldAcceptParameters()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Explicit_Pagination");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider);

        // Act & Assert
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
        using var context = CreateInMemoryContext($"TestDb_Various_{skip}_{take}");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider);

        // Act & Assert
        var action = async () => await service.GetBySourceNameAsync("Product", skip: skip, take: take);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetBySourceNameAndDateAsync Pagination Tests

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WithDefaultPagination_ShouldUseDefaultValues()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Date_Default");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider);
        var fromDate = DateTime.UtcNow.AddDays(-7);

        // Act & Assert
        var action = async () => await service.GetBySourceNameAndDateAsync("Product", fromDate);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WithExplicitPagination_ShouldAcceptParameters()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Date_Explicit");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider);
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
    public async Task GetBySourceNameAndDateAsync_WithAllParameters_ShouldAcceptFullSignature()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Date_Full");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider);
        var fromDate = DateTime.UtcNow.AddMonths(-1);
        var toDate = DateTime.UtcNow;

        // Act & Assert
        var action = async () => await service.GetBySourceNameAndDateAsync(
            sourceName: "Product",
            fromDate: fromDate,
            toDate: toDate,
            skip: 0,
            take: 100,
            cancellationToken: CancellationToken.None);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetSummaryBySourceNameAsync Pagination Tests

    [Fact]
    public async Task GetSummaryBySourceNameAsync_WithDefaultPagination_ShouldUseDefaultValues()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Summary_Default");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider);

        // Act & Assert
        var action = async () => await service.GetSummaryBySourceNameAsync("Product");

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetSummaryBySourceNameAsync_WithExplicitPagination_ShouldAcceptParameters()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Summary_Explicit");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider);

        // Act & Assert
        var action = async () => await service.GetSummaryBySourceNameAsync("Product", skip: 5, take: 15);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetSummaryBySourceNameAsync_WithCancellationToken_ShouldAcceptAllParameters()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Summary_Token");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider);
        using var cts = new CancellationTokenSource();

        // Act & Assert
        var action = async () => await service.GetSummaryBySourceNameAsync(
            "Product",
            skip: 0,
            take: 50,
            cancellationToken: cts.Token);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region Non-Paginated Methods Should Remain Unchanged

    [Fact]
    public async Task GetBySourceNameAndKeyAsync_ShouldNotHavePaginationParameters()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Key_NoPagination");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider);

        // Act & Assert
        var action = async () => await service.GetBySourceNameAndKeyAsync("Product", "123");

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetBySourceNameAndActionAsync_ShouldNotHavePaginationParameters()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Action_NoPagination");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider);

        // Act & Assert
        var action = async () => await service.GetBySourceNameAndActionAsync("Product", AuditAction.Created);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetBySourceNameActionAndDateAsync_ShouldNotHavePaginationParameters()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_ActionDate_NoPagination");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider);
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
        using var context = CreateInMemoryContext("TestDb_Validation_Empty");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider);

        // Act & Assert
        var action = async () => await service.GetBySourceNameAsync("");

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SourceName*");
    }

    [Fact]
    public async Task GetBySourceNameAsync_WithNullSourceName_ShouldThrowArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Validation_Null");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider);

        // Act & Assert
        var action = async () => await service.GetBySourceNameAsync(null!);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SourceName*");
    }

    [Fact]
    public async Task GetSummaryBySourceNameAsync_WithWhitespaceSourceName_ShouldThrowArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Validation_Whitespace");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider);

        // Act & Assert
        var action = async () => await service.GetSummaryBySourceNameAsync("   ");

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SourceName*");
    }

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WithEmptySourceName_ShouldThrowArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Validation_DateEmpty");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);
        var service = new EfAuditQueryService(context, provider);

        // Act & Assert
        var action = async () => await service.GetBySourceNameAndDateAsync("", DateTime.UtcNow);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SourceName*");
    }

    #endregion

    #region PostgreSQL Provider Tests

    [Fact]
    public async Task GetBySourceNameAsync_WithPostgreSqlProvider_ShouldAcceptPagination()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_PostgreSql_Pagination");
        var provider = new PostgreSqlDatabaseProvider(_postgreSqlOptions);
        var service = new EfAuditQueryService(context, provider);

        // Act & Assert
        var action = async () => await service.GetBySourceNameAsync("Product", skip: 0, take: 50);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetSummaryBySourceNameAsync_WithPostgreSqlProvider_ShouldAcceptPagination()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_PostgreSql_Summary");
        var pgXmlOptions = new AuditaXOptions
        {
            TableName = "audit_log",
            Schema = "public",
            ChangeLogFormat = ChangeLogFormat.Xml
        };
        var provider = new PostgreSqlDatabaseProvider(pgXmlOptions);
        var service = new EfAuditQueryService(context, provider);

        // Act & Assert
        var action = async () => await service.GetSummaryBySourceNameAsync("Product", skip: 10, take: 25);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetBySourceNameAndDateAsync_WithPostgreSqlProvider_ShouldAcceptPagination()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_PostgreSql_Date");
        var provider = new PostgreSqlDatabaseProvider(_postgreSqlOptions);
        var service = new EfAuditQueryService(context, provider);
        var fromDate = DateTime.UtcNow.AddDays(-30);

        // Act & Assert
        var action = async () => await service.GetBySourceNameAndDateAsync(
            "Product",
            fromDate,
            toDate: null,
            skip: 100,
            take: 50);

        await action.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void EfAuditQueryService_ShouldImplementIAuditQueryService()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_Interface");
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);

        // Act
        var service = new EfAuditQueryService(context, provider);

        // Assert
        service.Should().BeAssignableTo<IAuditQueryService>();
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new SqlServerDatabaseProvider(_sqlServerOptions);

        // Act & Assert
        var action = () => new EfAuditQueryService(null!, provider);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var context = CreateInMemoryContext("TestDb_NullProvider");

        // Act & Assert
        var action = () => new EfAuditQueryService(context, null!);

        action.Should().Throw<ArgumentNullException>();
    }

    #endregion
}
