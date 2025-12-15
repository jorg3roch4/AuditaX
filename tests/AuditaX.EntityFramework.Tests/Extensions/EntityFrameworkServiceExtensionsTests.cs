using AuditaX.Configuration;
using AuditaX.EntityFramework.Extensions;
using AuditaX.EntityFramework.Interceptors;
using AuditaX.EntityFramework.Tests.TestEntities;
using AuditaX.Extensions;
using AuditaX.Interfaces;
using AuditaX.SqlServer.Extensions;
using AuditaX.PostgreSql.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuditaX.EntityFramework.Tests.Extensions;

public class EntityFrameworkServiceExtensionsTests
{
    #region SQL Server Tests

    [Fact]
    public void UseEntityFramework_WithSqlServer_WithContext_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Register InMemory DbContext for testing
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb_SqlServer"));

        // Act - New extensible API: UseEntityFramework<TContext>() + UseSqlServer()
        services.AddAuditaX(options =>
        {
            options.TableName = "AuditLog";
            options.Schema = "dbo";
        })
        .UseEntityFramework<TestDbContext>()
        .UseSqlServer();

        var provider = services.BuildServiceProvider();

        // Assert - Services are registered
        provider.GetService<IDatabaseProvider>().Should().NotBeNull();
        provider.GetService<AuditSaveChangesInterceptor>().Should().NotBeNull();
    }

    #endregion

    #region PostgreSQL Tests

    [Fact]
    public void UseEntityFramework_WithPostgreSql_WithContext_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Register InMemory DbContext for testing
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb_PostgreSql"));

        // Act - New extensible API: UseEntityFramework<TContext>() + UsePostgreSql()
        services.AddAuditaX(options =>
        {
            options.TableName = "audit_log";
            options.Schema = "public";
        })
        .UseEntityFramework<TestDbContext>()
        .UsePostgreSql();

        var provider = services.BuildServiceProvider();

        // Assert - Services are registered
        provider.GetService<IDatabaseProvider>().Should().NotBeNull();
        provider.GetService<AuditSaveChangesInterceptor>().Should().NotBeNull();
    }

    #endregion

    #region Interceptor Tests

    [Fact]
    public void AddAuditaXInterceptor_WithSqlServer_ShouldAddToOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddAuditaX(options =>
        {
            options.ConfigureEntities(e =>
            {
                e.AuditEntity<Product>("Product")
                    .WithKey(p => p.Id)
                    .AuditProperties("Name");
            });
        })
        .UseEntityFramework<TestDbContext>()
        .UseSqlServer();

        // Register DbContext with interceptor
        services.AddDbContext<TestDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase("TestDb_Interceptor_SqlServer");
            options.AddAuditaXInterceptor(sp);
        });

        var provider = services.BuildServiceProvider();

        // Act - Create a scope to resolve DbContext
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // Assert - Just verify it doesn't throw
        context.Should().NotBeNull();
    }

    [Fact]
    public void AddAuditaXInterceptor_WithPostgreSql_ShouldAddToOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddAuditaX(options =>
        {
            options.ConfigureEntities(e =>
            {
                e.AuditEntity<Product>("Product")
                    .WithKey(p => p.Id)
                    .AuditProperties("Name");
            });
        })
        .UseEntityFramework<TestDbContext>()
        .UsePostgreSql();

        // Register DbContext with interceptor
        services.AddDbContext<TestDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase("TestDb_Interceptor_PostgreSql");
            options.AddAuditaXInterceptor(sp);
        });

        var provider = services.BuildServiceProvider();

        // Act - Create a scope to resolve DbContext
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // Assert - Just verify it doesn't throw
        context.Should().NotBeNull();
    }

    #endregion

    #region Fluent API Tests

    [Fact]
    public void FluentApi_ShouldAllowChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb_Chaining"));

        // Act - Test fluent API chaining
        var builder = services.AddAuditaX(options =>
        {
            options.TableName = "AuditLog";
            options.Schema = "dbo";
        })
        .UseEntityFramework<TestDbContext>()
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

        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb_PostgreSql_Chaining"));

        // Act - Test fluent API chaining with PostgreSQL
        var builder = services.AddAuditaX(options =>
        {
            options.TableName = "audit_log";
            options.Schema = "public";
        })
        .UseEntityFramework<TestDbContext>()
        .UsePostgreSql()
        .ValidateOnStartup();

        // Assert
        builder.Should().NotBeNull();
        builder.IsStartupValidationEnabled.Should().BeTrue();
    }

    #endregion
}
