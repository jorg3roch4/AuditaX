using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.Extensions;
using AuditaX.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuditaX.Tests.Extensions;

public class ServiceExtensionsTests
{
    [Fact]
    public void AddAuditaX_WithFluentConfig_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAuditaX(options =>
        {
            options.TableName = "CustomAuditLog";
            options.Schema = "audit";
            options.LogFormat = LogFormat.Json;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<AuditaXOptions>();
        options.TableName.Should().Be("CustomAuditLog");
        options.Schema.Should().Be("audit");
        options.LogFormat.Should().Be(LogFormat.Json);

        provider.GetService<IChangeLogService>().Should().NotBeNull();
        // Note: IAuditService is registered but requires IAuditRepository from an ORM provider
        // It will only resolve when UseEntityFramework or UseDapper is configured
        provider.GetService<IAuditUserProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddAuditaX_WithConfiguration_ShouldReadFromAppSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AuditaX:TableName", "ConfiguredTable" },
                { "AuditaX:Schema", "custom" },
                { "AuditaX:AutoCreateTable", "true" },
                { "AuditaX:EnableLogging", "true" },
                { "AuditaX:LogFormat", "Json" }
            })
            .Build();

        // Act
        services.AddAuditaX(configuration);

        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<AuditaXOptions>();
        options.TableName.Should().Be("ConfiguredTable");
        options.Schema.Should().Be("custom");
        options.AutoCreateTable.Should().BeTrue();
        options.EnableLogging.Should().BeTrue();
        options.LogFormat.Should().Be(LogFormat.Json);
    }

    [Fact]
    public void AddAuditaX_WithConfigurationAndFluent_ShouldMerge()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AuditaX:TableName", "FromConfig" },
                { "AuditaX:Schema", "config_schema" }
            })
            .Build();

        // Act
        services.AddAuditaX(configuration, options =>
        {
            // Override some settings
            options.TableName = "FromFluent";
            options.AutoCreateTable = true;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<AuditaXOptions>();
        options.TableName.Should().Be("FromFluent"); // Overridden by fluent
        options.Schema.Should().Be("config_schema"); // From config
        options.AutoCreateTable.Should().BeTrue(); // From fluent
    }

    [Fact]
    public void AddAuditaX_ShouldReturnBuilder()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddAuditaX(options => { });

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<AuditaXBuilder>();
        builder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void UseUserProvider_ShouldOverrideDefault()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAuditaX(options => { })
            .UseUserProvider<CustomUserProvider>();

        var provider = services.BuildServiceProvider();

        // Assert
        var userProvider = provider.GetRequiredService<IAuditUserProvider>();
        userProvider.GetCurrentUser().Should().Be("custom-user");
    }

    [Fact]
    public void UseUserProvider_WithFactory_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAuditaX(options => { })
            .UseUserProvider(sp => new CustomUserProvider());

        var provider = services.BuildServiceProvider();

        // Assert
        var userProvider = provider.GetRequiredService<IAuditUserProvider>();
        userProvider.GetCurrentUser().Should().Be("custom-user");
    }

    [Fact]
    public void LogFormat_Xml_ShouldProduceXml()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuditaX(options =>
        {
            options.LogFormat = LogFormat.Xml;
        });

        var provider = services.BuildServiceProvider();
        var changeLogService = provider.GetRequiredService<IChangeLogService>();

        // Act
        var result = changeLogService.CreateEntry(null, "test");

        // Assert
        result.Should().StartWith("<AuditLog>");
    }

    [Fact]
    public void LogFormat_Json_ShouldProduceJson()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuditaX(options =>
        {
            options.LogFormat = LogFormat.Json;
        });

        var provider = services.BuildServiceProvider();
        var changeLogService = provider.GetRequiredService<IChangeLogService>();

        // Act
        var result = changeLogService.CreateEntry(null, "test");

        // Assert
        result.Should().StartWith("{\"auditLog\"");
    }

    // Test user provider
    private class CustomUserProvider : IAuditUserProvider
    {
        public string GetCurrentUser() => "custom-user";
    }
}
