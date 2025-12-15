using AuditaX.Configuration;
using AuditaX.Extensions;
using AuditaX.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AuditaX.Tests.Extensions;

public class AuditaXBuilderTests
{
    [Fact]
    public void AuditaXBuilder_InitialState_ShouldNotBeConfigured()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddAuditaX(options => { });

        // Assert
        builder.IsStartupValidationEnabled.Should().BeFalse();
    }

    [Fact]
    public void Services_ShouldReturnSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddAuditaX(options => { });

        // Assert
        builder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void Options_ShouldReturnConfiguredOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddAuditaX(options =>
        {
            options.TableName = "TestTable";
            options.Schema = "TestSchema";
        });

        // Assert
        builder.Options.TableName.Should().Be("TestTable");
        builder.Options.Schema.Should().Be("TestSchema");
    }

    [Fact]
    public void UseUserProvider_Generic_ShouldRegisterProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAuditaX(options => { })
            .UseUserProvider<TestUserProvider>();

        var provider = services.BuildServiceProvider();

        // Assert
        var userProvider = provider.GetRequiredService<IAuditUserProvider>();
        userProvider.Should().BeOfType<TestUserProvider>();
        userProvider.GetCurrentUser().Should().Be("test-user");
    }

    [Fact]
    public void UseUserProvider_WithFactory_ShouldRegisterProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var customUser = "factory-user";

        // Act
        services.AddAuditaX(options => { })
            .UseUserProvider(sp => new FactoryUserProvider(customUser));

        var provider = services.BuildServiceProvider();

        // Assert
        var userProvider = provider.GetRequiredService<IAuditUserProvider>();
        userProvider.Should().BeOfType<FactoryUserProvider>();
        userProvider.GetCurrentUser().Should().Be(customUser);
    }

    [Fact]
    public void ValidateOnStartup_ShouldEnableStartupValidation()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddAuditaX(options => { })
            .ValidateOnStartup();

        // Assert
        builder.IsStartupValidationEnabled.Should().BeTrue();
    }

    [Fact]
    public void ValidateOnStartup_CalledTwice_ShouldOnlyRegisterOnce()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddAuditaX(options => { })
            .ValidateOnStartup()
            .ValidateOnStartup();

        // Assert
        builder.IsStartupValidationEnabled.Should().BeTrue();
        // Count hosted services - should only be one AuditaXStartupHostedService
        var hostedServiceCount = services.Count(d =>
            d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService));
        hostedServiceCount.Should().Be(1);
    }

    [Fact]
    public void FluentChaining_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Chain multiple methods
        var builder = services.AddAuditaX(options =>
        {
            options.TableName = "AuditLog";
        })
        .UseUserProvider<TestUserProvider>()
        .ValidateOnStartup();

        // Assert
        builder.Should().NotBeNull();
        builder.Options.TableName.Should().Be("AuditLog");
        builder.IsStartupValidationEnabled.Should().BeTrue();
    }

    // Test user providers
    private class TestUserProvider : IAuditUserProvider
    {
        public string GetCurrentUser() => "test-user";
    }

    private class FactoryUserProvider(string user) : IAuditUserProvider
    {
        public string GetCurrentUser() => user;
    }
}
