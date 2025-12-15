using System.Data;
using AuditaX.Dapper.Extensions;
using AuditaX.Extensions;
using AuditaX.Interfaces;
using AuditaX.SqlServer.Extensions;
using AuditaX.PostgreSql.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AuditaX.Dapper.Tests.Extensions;

public class DapperServiceExtensionsTests
{
    #region SQL Server Tests

    [Fact]
    public void UseDapper_WithSqlServer_WithContext_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConnection = new Mock<IDbConnection>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);

        // Register a mock DapperContext
        services.AddScoped<MockDapperContext>(_ => new MockDapperContext(mockConnection.Object));

        // Act - New extensible API: UseDapper<TContext>() + UseSqlServer()
        services.AddAuditaX(options =>
        {
            options.TableName = "AuditLog";
            options.Schema = "dbo";
        })
        .UseDapper<MockDapperContext>()
        .UseSqlServer();

        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IAuditRepository>().Should().NotBeNull();
        provider.GetService<IDatabaseProvider>().Should().NotBeNull();
    }

    [Fact]
    public void UseDapper_WithSqlServer_WithFactory_ShouldRegisterRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConnection = new Mock<IDbConnection>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);

        // Act - Factory overload with mock + UseSqlServer()
        services.AddAuditaX(options =>
        {
            options.TableName = "AuditLog";
            options.Schema = "dbo";
        })
        .UseDapper(sp => mockConnection.Object)
        .UseSqlServer();

        var provider = services.BuildServiceProvider();

        // Assert - With mock we can resolve everything
        provider.GetService<IAuditRepository>().Should().NotBeNull();
        provider.GetService<IDatabaseProvider>().Should().NotBeNull();
    }

    #endregion

    #region PostgreSQL Tests

    [Fact]
    public void UseDapper_WithPostgreSql_WithContext_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConnection = new Mock<IDbConnection>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);

        // Register a mock DapperContext
        services.AddScoped<MockDapperContext>(_ => new MockDapperContext(mockConnection.Object));

        // Act - New extensible API: UseDapper<TContext>() + UsePostgreSql()
        services.AddAuditaX(options =>
        {
            options.TableName = "audit_log";
            options.Schema = "public";
        })
        .UseDapper<MockDapperContext>()
        .UsePostgreSql();

        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IAuditRepository>().Should().NotBeNull();
        provider.GetService<IDatabaseProvider>().Should().NotBeNull();
    }

    [Fact]
    public void UseDapper_WithPostgreSql_WithFactory_ShouldRegisterRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConnection = new Mock<IDbConnection>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);

        services.AddScoped<IDbConnection>(_ => mockConnection.Object);

        // Act - Factory overload + UsePostgreSql()
        services.AddAuditaX(options =>
        {
            options.TableName = "audit_log";
            options.Schema = "public";
        })
        .UseDapper(sp => sp.GetRequiredService<IDbConnection>())
        .UsePostgreSql();

        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IAuditRepository>().Should().NotBeNull();
        provider.GetService<IDatabaseProvider>().Should().NotBeNull();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void UseDapper_WithNullFactory_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, IDbConnection>? nullFactory = null;

        // Act & Assert
        var action = () => services.AddAuditaX(options => { })
            .UseDapper(nullFactory!);

        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Repository Type Tests

    [Fact]
    public void UseDapper_ShouldRegisterDapperRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConnection = new Mock<IDbConnection>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);

        // Act
        services.AddAuditaX(options =>
        {
            options.TableName = "AuditLog";
        })
        .UseDapper(sp => mockConnection.Object)
        .UseSqlServer();

        var provider = services.BuildServiceProvider();

        // Assert - Verify registration by resolving (uses mock, no real connection)
        var repository = provider.GetService<IAuditRepository>();
        repository.Should().NotBeNull();
        repository!.GetType().Name.Should().Be("DapperAuditRepository");
    }

    [Fact]
    public void UseDapper_ShouldRegisterStartupValidator()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConnection = new Mock<IDbConnection>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);

        // Act
        services.AddAuditaX(options =>
        {
            options.TableName = "AuditLog";
        })
        .UseDapper(sp => mockConnection.Object)
        .UseSqlServer();

        var provider = services.BuildServiceProvider();

        // Assert - Verify registration by resolving (uses mock, no real connection)
        var validator = provider.GetService<IAuditStartupValidator>();
        validator.Should().NotBeNull();
        validator!.GetType().Name.Should().Be("DapperAuditStartupValidator");
    }

    #endregion

    #region Fluent API Tests

    [Fact]
    public void FluentApi_ShouldAllowChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConnection = new Mock<IDbConnection>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);
        services.AddScoped<MockDapperContext>(_ => new MockDapperContext(mockConnection.Object));

        // Act - Test fluent API chaining
        var builder = services.AddAuditaX(options =>
        {
            options.TableName = "AuditLog";
            options.Schema = "dbo";
        })
        .UseDapper<MockDapperContext>()
        .UseSqlServer()
        .ValidateOnStartup();

        // Assert
        builder.Should().NotBeNull();
        builder.IsStartupValidationEnabled.Should().BeTrue();
    }

    [Fact]
    public void FluentApi_PostgreSql_ShouldAllowChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConnection = new Mock<IDbConnection>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);
        services.AddScoped<MockDapperContext>(_ => new MockDapperContext(mockConnection.Object));

        // Act - Test fluent API chaining with PostgreSQL
        var builder = services.AddAuditaX(options =>
        {
            options.TableName = "audit_log";
            options.Schema = "public";
        })
        .UseDapper<MockDapperContext>()
        .UsePostgreSql()
        .ValidateOnStartup();

        // Assert
        builder.Should().NotBeNull();
        builder.IsStartupValidationEnabled.Should().BeTrue();
    }

    #endregion
}

/// <summary>
/// Mock DapperContext for testing that uses a mock IDbConnection.
/// </summary>
public class MockDapperContext
{
    private readonly IDbConnection _mockConnection;

    public MockDapperContext(IDbConnection mockConnection)
    {
        _mockConnection = mockConnection;
    }

    public IDbConnection CreateConnection()
    {
        return _mockConnection;
    }
}
