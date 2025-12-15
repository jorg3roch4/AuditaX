using AuditaX.Configuration;
using AuditaX.Enums;
using Microsoft.Extensions.Logging;

namespace AuditaX.Tests.Configuration;

public class AuditaXOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new AuditaXOptions();

        // Assert
        options.EnableLogging.Should().BeFalse();
        options.MinimumLogLevel.Should().Be(LogLevel.Information);
        options.TableName.Should().Be("AuditLog");
        options.Schema.Should().Be("dbo");
        options.AutoCreateTable.Should().BeFalse();
        options.ChangeLogFormat.Should().Be(ChangeLogFormat.Xml);
    }

    [Fact]
    public void ConfigureEntities_ShouldRegisterEntity()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act
        options.ConfigureEntities(builder =>
        {
            builder.AuditEntity<TestEntity>("TestEntity")
                .WithKey(e => e.Id)
                .AuditProperties("Name");
        });

        // Assert
        options.IsAuditableEntity(typeof(TestEntity)).Should().BeTrue();
        options.GetEntityConfiguration(typeof(TestEntity)).Should().NotBeNull();
    }

    [Fact]
    public void ConfigureEntities_WithRelated_ShouldRegisterBoth()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act
        options.ConfigureEntities(builder =>
        {
            builder.AuditEntity<TestEntity>("TestEntity")
                .WithKey(e => e.Id)
                .AuditProperties("Name")
                .WithRelatedEntity<TestRelatedEntity>("TestRelated")
                .WithParentKey(r => r.ParentId)
                .OnAdded(r => new Dictionary<string, string?> { ["Value"] = r.Value });
        });

        // Assert
        options.IsAuditableEntity(typeof(TestEntity)).Should().BeTrue();
        options.IsRelatedEntity(typeof(TestRelatedEntity)).Should().BeTrue();
    }

    [Fact]
    public void GetEntityConfiguration_ForUnregistered_ShouldReturnNull()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act
        var config = options.GetEntityConfiguration(typeof(TestEntity));

        // Assert
        config.Should().BeNull();
    }

    [Fact]
    public void IsAuditableEntity_ForUnregistered_ShouldReturnFalse()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act & Assert
        options.IsAuditableEntity(typeof(TestEntity)).Should().BeFalse();
    }

    [Fact]
    public void ChangeLogFormat_Json_ShouldBeConfigurable()
    {
        // Arrange & Act
        var options = new AuditaXOptions
        {
            ChangeLogFormat = ChangeLogFormat.Json
        };

        // Assert
        options.ChangeLogFormat.Should().Be(ChangeLogFormat.Json);
    }

    [Fact]
    public void ChangeLogFormat_Xml_ShouldBeConfigurable()
    {
        // Arrange & Act
        var options = new AuditaXOptions
        {
            ChangeLogFormat = ChangeLogFormat.Xml
        };

        // Assert
        options.ChangeLogFormat.Should().Be(ChangeLogFormat.Xml);
    }

    [Fact]
    public void TableName_ShouldBeConfigurable()
    {
        // Arrange & Act
        var options = new AuditaXOptions
        {
            TableName = "CustomAuditTable"
        };

        // Assert
        options.TableName.Should().Be("CustomAuditTable");
    }

    [Fact]
    public void Schema_ShouldBeConfigurable()
    {
        // Arrange & Act
        var options = new AuditaXOptions
        {
            Schema = "audit"
        };

        // Assert
        options.Schema.Should().Be("audit");
    }

    [Fact]
    public void AutoCreateTable_ShouldBeConfigurable()
    {
        // Arrange & Act
        var options = new AuditaXOptions
        {
            AutoCreateTable = true
        };

        // Assert
        options.AutoCreateTable.Should().BeTrue();
    }

    [Fact]
    public void EnableLogging_ShouldBeConfigurable()
    {
        // Arrange & Act
        var options = new AuditaXOptions
        {
            EnableLogging = true
        };

        // Assert
        options.EnableLogging.Should().BeTrue();
    }

    [Fact]
    public void MinimumLogLevel_ShouldBeConfigurable()
    {
        // Arrange & Act
        var options = new AuditaXOptions
        {
            MinimumLogLevel = LogLevel.Debug
        };

        // Assert
        options.MinimumLogLevel.Should().Be(LogLevel.Debug);
    }

    [Fact]
    public void ConfigureEntities_MultipleEntities_ShouldRegisterAll()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act
        options.ConfigureEntities(builder =>
        {
            builder.AuditEntity<TestEntity>("TestEntity")
                .WithKey(e => e.Id)
                .AuditProperties("Name");

            builder.AuditEntity<AnotherTestEntity>("AnotherTestEntity")
                .WithKey(e => e.Id)
                .AuditProperties("Description");
        });

        // Assert
        options.IsAuditableEntity(typeof(TestEntity)).Should().BeTrue();
        options.IsAuditableEntity(typeof(AnotherTestEntity)).Should().BeTrue();
    }

    [Fact]
    public void IsRelatedEntity_ForUnregistered_ShouldReturnFalse()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act & Assert
        options.IsRelatedEntity(typeof(TestRelatedEntity)).Should().BeFalse();
    }

    // Test entities
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class AnotherTestEntity
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    private class TestRelatedEntity
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}
