using AuditaX.Configuration;
using AuditaX.Enums;

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
        options.TableName.Should().Be("AuditLog");
        options.Schema.Should().Be("dbo");
        options.AutoCreateTable.Should().BeFalse();
        options.LogFormat.Should().Be(LogFormat.Xml);
    }

    [Fact]
    public void ConfigureEntity_ShouldRegisterEntity()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act
        options.ConfigureEntity<TestEntity>("TestEntity")
            .WithKey(e => e.Id)
            .Properties("Name");

        // Assert
        options.GetEntity(typeof(TestEntity)).Should().NotBeNull();
    }

    [Fact]
    public void ConfigureEntity_WithRelated_ShouldRegisterBoth()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act
        options.ConfigureEntity<TestEntity>("TestEntity")
            .WithKey(e => e.Id)
            .Properties("Name")
            .WithRelatedEntity<TestRelatedEntity>("TestRelated")
            .WithParentKey(r => r.ParentId)
            .Properties("Value");

        // Assert
        options.GetEntity(typeof(TestEntity)).Should().NotBeNull();
        options.GetRelatedEntity(typeof(TestRelatedEntity)).Should().NotBeNull();
    }

    [Fact]
    public void GetEntity_ForUnregistered_ShouldReturnNull()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act
        var config = options.GetEntity(typeof(TestEntity));

        // Assert
        config.Should().BeNull();
    }

    [Fact]
    public void LogFormat_Json_ShouldBeConfigurable()
    {
        // Arrange & Act
        var options = new AuditaXOptions
        {
            LogFormat = LogFormat.Json
        };

        // Assert
        options.LogFormat.Should().Be(LogFormat.Json);
    }

    [Fact]
    public void LogFormat_Xml_ShouldBeConfigurable()
    {
        // Arrange & Act
        var options = new AuditaXOptions
        {
            LogFormat = LogFormat.Xml
        };

        // Assert
        options.LogFormat.Should().Be(LogFormat.Xml);
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
    public void ConfigureEntity_MultipleEntities_ShouldRegisterAll()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act
        options.ConfigureEntity<TestEntity>("TestEntity")
            .WithKey(e => e.Id)
            .Properties("Name");

        options.ConfigureEntity<AnotherTestEntity>("AnotherTestEntity")
            .WithKey(e => e.Id)
            .Properties("Description");

        // Assert
        options.GetEntity(typeof(TestEntity)).Should().NotBeNull();
        options.GetEntity(typeof(AnotherTestEntity)).Should().NotBeNull();
    }

    [Fact]
    public void GetRelatedEntity_ForUnregistered_ShouldReturnNull()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act & Assert
        options.GetRelatedEntity(typeof(TestRelatedEntity)).Should().BeNull();
    }

    [Fact]
    public void ConfigureEntity_WithLookup_ShouldRegisterLookup()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act
        options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .Properties("UserName")
            .WithRelatedEntity<TestUserRole>("UserRoles")
                .WithParentKey(ur => ur.UserId)
                .WithLookup<TestRole>("Roles")
                    .ForeignKey(ur => ur.RoleId)
                    .Key(r => r.RoleId)
                    .Properties("RoleName");

        // Assert
        var relatedOptions = options.GetRelatedEntity(typeof(TestUserRole));
        relatedOptions.Should().NotBeNull();
        relatedOptions!.HasLookups.Should().BeTrue();
        relatedOptions.Lookups.Should().ContainKey("Roles");
    }

    [Fact]
    public void ConfigureEntity_WithLookup_ShouldSetLookupProperties()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act
        options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .Properties("UserName")
            .WithRelatedEntity<TestUserRole>("UserRoles")
                .WithParentKey(ur => ur.UserId)
                .WithLookup<TestRole>("Roles")
                    .ForeignKey(ur => ur.RoleId)
                    .Key(r => r.RoleId)
                    .Properties("RoleName");

        // Assert
        var relatedOptions = options.GetRelatedEntity(typeof(TestUserRole));
        var lookupOptions = relatedOptions!.Lookups["Roles"];

        lookupOptions.ForeignKey.Should().Be("RoleId");
        lookupOptions.Key.Should().Be("RoleId");
        lookupOptions.Properties.Should().Contain("RoleName");
        lookupOptions.EntityType.Should().Be(typeof(TestRole));
        lookupOptions.EntityName.Should().Be("Roles");
    }

    [Fact]
    public void LookupOptions_GetForeignKeyValue_ShouldReturnCorrectValue()
    {
        // Arrange
        var options = new AuditaXOptions();
        options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .WithRelatedEntity<TestUserRole>("UserRoles")
                .WithParentKey(ur => ur.UserId)
                .WithLookup<TestRole>("Roles")
                    .ForeignKey(ur => ur.RoleId)
                    .Key(r => r.RoleId)
                    .Properties("RoleName");

        var userRole = new TestUserRole { UserId = "user-1", RoleId = "role-admin" };
        var relatedOptions = options.GetRelatedEntity(typeof(TestUserRole));
        var lookupOptions = relatedOptions!.Lookups["Roles"];

        // Act
        var foreignKeyValue = lookupOptions.GetForeignKeyValue(userRole);

        // Assert
        foreignKeyValue.Should().Be("role-admin");
    }

    [Fact]
    public void LookupOptions_GetPropertyValues_ShouldReturnCorrectValues()
    {
        // Arrange
        var options = new AuditaXOptions();
        options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .WithRelatedEntity<TestUserRole>("UserRoles")
                .WithParentKey(ur => ur.UserId)
                .WithLookup<TestRole>("Roles")
                    .ForeignKey(ur => ur.RoleId)
                    .Key(r => r.RoleId)
                    .Properties("RoleName");

        var role = new TestRole { RoleId = "role-admin", RoleName = "Administrator" };
        var relatedOptions = options.GetRelatedEntity(typeof(TestUserRole));
        var lookupOptions = relatedOptions!.Lookups["Roles"];

        // Act
        var propertyValues = lookupOptions.GetPropertyValues(role);

        // Assert
        propertyValues.Should().ContainKey("RoleName");
        propertyValues["RoleName"].Should().Be("Administrator");
    }

    [Fact]
    public void RelatedEntity_WithoutLookups_HasLookupsShouldBeFalse()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act
        options.ConfigureEntity<TestEntity>("TestEntity")
            .WithKey(e => e.Id)
            .Properties("Name")
            .WithRelatedEntity<TestRelatedEntity>("TestRelated")
                .WithParentKey(r => r.ParentId)
                .Properties("Value");

        // Assert
        var relatedOptions = options.GetRelatedEntity(typeof(TestRelatedEntity));
        relatedOptions.Should().NotBeNull();
        relatedOptions!.HasLookups.Should().BeFalse();
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

    // Test entities for Lookup tests (ASP.NET Identity-like)
    private class TestUser
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }

    private class TestRole
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    private class TestUserRole
    {
        public string UserId { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
    }
}
