using AuditaX.Configuration;

namespace AuditaX.Tests.Configuration;

/// <summary>
/// Tests for LookupOptions configuration and resolution.
/// </summary>
public class LookupOptionsTests
{
    // ══════════════════════════════════════════════════════════
    // FluentAPI Tests
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void FluentApi_WithLookup_ShouldRegisterLookupInRelatedEntity()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act
        options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .Properties("UserName", "Email")
            .WithRelatedEntity<TestUserRole>("UserRoles")
                .WithParentKey(ur => ur.UserId)
                .WithLookup<TestRole>("Role")
                    .ForeignKey(ur => ur.RoleId)
                    .Key(r => r.RoleId)
                    .Properties("RoleName");

        // Assert
        var relatedOptions = options.GetRelatedEntity(typeof(TestUserRole));
        relatedOptions.Should().NotBeNull();
        relatedOptions!.HasLookups.Should().BeTrue();
        relatedOptions.Lookups.Should().ContainKey("Role");
    }

    [Fact]
    public void FluentApi_WithLookup_ShouldSetEntityType()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act
        options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .WithRelatedEntity<TestUserRole>("UserRoles")
                .WithParentKey(ur => ur.UserId)
                .WithLookup<TestRole>("Role")
                    .ForeignKey(ur => ur.RoleId)
                    .Key(r => r.RoleId)
                    .Properties("RoleName");

        // Assert
        var lookupOptions = options.GetRelatedEntity(typeof(TestUserRole))!.Lookups["Role"];
        lookupOptions.EntityType.Should().Be(typeof(TestRole));
        lookupOptions.EntityName.Should().Be("Role");
    }

    [Fact]
    public void FluentApi_WithLookup_ShouldSetForeignKeySelector()
    {
        // Arrange
        var options = new AuditaXOptions();
        options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .WithRelatedEntity<TestUserRole>("UserRoles")
                .WithParentKey(ur => ur.UserId)
                .WithLookup<TestRole>("Role")
                    .ForeignKey(ur => ur.RoleId)
                    .Key(r => r.RoleId)
                    .Properties("RoleName");

        var userRole = new TestUserRole { UserId = "user-1", RoleId = "role-admin" };
        var lookupOptions = options.GetRelatedEntity(typeof(TestUserRole))!.Lookups["Role"];

        // Act
        var foreignKeyValue = lookupOptions.GetForeignKeyValue(userRole);

        // Assert
        foreignKeyValue.Should().Be("role-admin");
    }

    [Fact]
    public void FluentApi_WithLookup_ShouldSetKeySelector()
    {
        // Arrange
        var options = new AuditaXOptions();
        options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .WithRelatedEntity<TestUserRole>("UserRoles")
                .WithParentKey(ur => ur.UserId)
                .WithLookup<TestRole>("Role")
                    .ForeignKey(ur => ur.RoleId)
                    .Key(r => r.RoleId)
                    .Properties("RoleName");

        var role = new TestRole { RoleId = "role-admin", RoleName = "Administrator" };
        var lookupOptions = options.GetRelatedEntity(typeof(TestUserRole))!.Lookups["Role"];

        // Act
        var keyValue = lookupOptions.GetKeyValue(role);

        // Assert
        keyValue.Should().Be("role-admin");
    }

    [Fact]
    public void FluentApi_WithLookup_ShouldGetPropertyValues()
    {
        // Arrange
        var options = new AuditaXOptions();
        options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .WithRelatedEntity<TestUserRole>("UserRoles")
                .WithParentKey(ur => ur.UserId)
                .WithLookup<TestRole>("Role")
                    .ForeignKey(ur => ur.RoleId)
                    .Key(r => r.RoleId)
                    .Properties("RoleName", "Description");

        var role = new TestRole
        {
            RoleId = "role-admin",
            RoleName = "Administrator",
            Description = "Full access"
        };
        var lookupOptions = options.GetRelatedEntity(typeof(TestUserRole))!.Lookups["Role"];

        // Act
        var propertyValues = lookupOptions.GetPropertyValues(role);

        // Assert
        propertyValues.Should().ContainKey("RoleName");
        propertyValues["RoleName"].Should().Be("Administrator");
        propertyValues.Should().ContainKey("Description");
        propertyValues["Description"].Should().Be("Full access");
    }

    [Fact]
    public void FluentApi_WithMultipleLookups_ShouldRegisterAll()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act - UserRole with lookups to both Role and Department
        options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .WithRelatedEntity<TestUserRoleExtended>("UserRoles")
                .WithParentKey(ur => ur.UserId)
                .WithLookup<TestRole>("Role")
                    .ForeignKey(ur => ur.RoleId)
                    .Key(r => r.RoleId)
                    .Properties("RoleName")
                .WithLookup<TestDepartment>("Department")
                    .ForeignKey(ur => ur.DepartmentId)
                    .Key(d => d.DepartmentId)
                    .Properties("DepartmentName");

        // Assert
        var relatedOptions = options.GetRelatedEntity(typeof(TestUserRoleExtended));
        relatedOptions.Should().NotBeNull();
        relatedOptions!.Lookups.Should().HaveCount(2);
        relatedOptions.Lookups.Should().ContainKey("Role");
        relatedOptions.Lookups.Should().ContainKey("Department");
    }

    [Fact]
    public void FluentApi_IsResolved_ShouldBeTrueAfterFluentApiConfiguration()
    {
        // Arrange
        var options = new AuditaXOptions();

        // Act
        options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .WithRelatedEntity<TestUserRole>("UserRoles")
                .WithParentKey(ur => ur.UserId)
                .WithLookup<TestRole>("Role")
                    .ForeignKey(ur => ur.RoleId)
                    .Key(r => r.RoleId)
                    .Properties("RoleName");

        // Assert
        var lookupOptions = options.GetRelatedEntity(typeof(TestUserRole))!.Lookups["Role"];
        lookupOptions.IsResolved.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════
    // AppSettings Tests (simulated JSON configuration)
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void AppSettings_GetRelatedEntity_ShouldResolveFromEntityConfiguration()
    {
        // Arrange - Simulate appsettings.json structure
        var options = new AuditaXOptions();

        var relatedOptions = new RelatedEntityOptions
        {
            ParentKey = "UserId",
            Lookups = new Dictionary<string, LookupOptions>
            {
                ["Role"] = new LookupOptions
                {
                    ForeignKey = "RoleId",
                    Key = "RoleId",
                    Properties = ["RoleName"]
                }
            }
        };

        var entityOptions = new EntityOptions
        {
            Key = "UserId",
            Properties = ["UserName", "Email"],
            RelatedEntities = new Dictionary<string, RelatedEntityOptions>
            {
                ["TestUserRole"] = relatedOptions
            }
        };

        options.Entities["TestUser"] = entityOptions;

        // Act - Simulate interceptor calling GetRelatedEntity
        var resolved = options.GetRelatedEntity(typeof(TestUserRole));

        // Assert
        resolved.Should().NotBeNull();
        resolved!.EntityType.Should().Be(typeof(TestUserRole));
        resolved.RelatedName.Should().Be("TestUserRole");
        resolved.ParentEntityOptions.Should().Be(entityOptions);
        resolved.HasLookups.Should().BeTrue();
    }

    [Fact]
    public void AppSettings_GetRelatedEntity_ShouldSetLookupEntityNames()
    {
        // Arrange - Simulate appsettings.json structure
        var options = new AuditaXOptions();

        var relatedOptions = new RelatedEntityOptions
        {
            ParentKey = "UserId",
            Lookups = new Dictionary<string, LookupOptions>
            {
                ["Role"] = new LookupOptions
                {
                    ForeignKey = "RoleId",
                    Key = "RoleId",
                    Properties = ["RoleName"]
                }
            }
        };

        options.Entities["TestUser"] = new EntityOptions
        {
            Key = "UserId",
            RelatedEntities = new Dictionary<string, RelatedEntityOptions>
            {
                ["TestUserRole"] = relatedOptions
            }
        };

        // Act
        var resolved = options.GetRelatedEntity(typeof(TestUserRole));

        // Assert
        var lookupOptions = resolved!.Lookups["Role"];
        lookupOptions.EntityName.Should().Be("Role");
    }

    [Fact]
    public void AppSettings_GetRelatedEntity_ShouldResolveParentKeySelector()
    {
        // Arrange
        var options = new AuditaXOptions();

        options.Entities["TestUser"] = new EntityOptions
        {
            Key = "UserId",
            RelatedEntities = new Dictionary<string, RelatedEntityOptions>
            {
                ["TestUserRole"] = new RelatedEntityOptions
                {
                    ParentKey = "UserId",
                    Lookups = new Dictionary<string, LookupOptions>
                    {
                        ["Role"] = new LookupOptions
                        {
                            ForeignKey = "RoleId",
                            Key = "RoleId",
                            Properties = ["RoleName"]
                        }
                    }
                }
            }
        };

        var userRole = new TestUserRole { UserId = "user-123", RoleId = "role-admin" };

        // Act
        var resolved = options.GetRelatedEntity(typeof(TestUserRole));
        var parentKey = resolved!.GetParentKey(userRole);

        // Assert
        parentKey.Should().Be("user-123");
    }

    [Fact]
    public void AppSettings_Lookup_ShouldBeUnresolvedInitially()
    {
        // Arrange
        var options = new AuditaXOptions();

        options.Entities["TestUser"] = new EntityOptions
        {
            Key = "UserId",
            RelatedEntities = new Dictionary<string, RelatedEntityOptions>
            {
                ["TestUserRole"] = new RelatedEntityOptions
                {
                    ParentKey = "UserId",
                    Lookups = new Dictionary<string, LookupOptions>
                    {
                        ["Role"] = new LookupOptions
                        {
                            ForeignKey = "RoleId",
                            Key = "RoleId",
                            Properties = ["RoleName"]
                        }
                    }
                }
            }
        };

        // Act
        var resolved = options.GetRelatedEntity(typeof(TestUserRole));

        // Assert - EntityType is not set yet (will be resolved by interceptor from EF Model)
        var lookupOptions = resolved!.Lookups["Role"];
        lookupOptions.EntityType.Should().BeNull();
        lookupOptions.IsResolved.Should().BeFalse();
        resolved.HasUnresolvedLookups.Should().BeTrue();
    }

    [Fact]
    public void AppSettings_Lookup_ShouldResolveAfterCallingResolve()
    {
        // Arrange
        var options = new AuditaXOptions();

        options.Entities["TestUser"] = new EntityOptions
        {
            Key = "UserId",
            RelatedEntities = new Dictionary<string, RelatedEntityOptions>
            {
                ["TestUserRole"] = new RelatedEntityOptions
                {
                    ParentKey = "UserId",
                    Lookups = new Dictionary<string, LookupOptions>
                    {
                        ["Role"] = new LookupOptions
                        {
                            ForeignKey = "RoleId",
                            Key = "RoleId",
                            Properties = ["RoleName"]
                        }
                    }
                }
            }
        };

        var resolved = options.GetRelatedEntity(typeof(TestUserRole));
        var lookupOptions = resolved!.Lookups["Role"];

        // Act - Simulate what the interceptor does
        lookupOptions.Resolve(typeof(TestRole), typeof(TestUserRole));

        // Assert
        lookupOptions.EntityType.Should().Be(typeof(TestRole));
        lookupOptions.IsResolved.Should().BeTrue();
        resolved.HasUnresolvedLookups.Should().BeFalse();
    }

    [Fact]
    public void AppSettings_Lookup_AfterResolve_ShouldGetForeignKeyValue()
    {
        // Arrange
        var lookupOptions = new LookupOptions
        {
            ForeignKey = "RoleId",
            Key = "RoleId",
            Properties = ["RoleName"]
        };

        var userRole = new TestUserRole { UserId = "user-1", RoleId = "role-admin" };

        // Act
        lookupOptions.Resolve(typeof(TestRole), typeof(TestUserRole));
        var foreignKeyValue = lookupOptions.GetForeignKeyValue(userRole);

        // Assert
        foreignKeyValue.Should().Be("role-admin");
    }

    [Fact]
    public void AppSettings_Lookup_AfterResolve_ShouldGetKeyValue()
    {
        // Arrange
        var lookupOptions = new LookupOptions
        {
            ForeignKey = "RoleId",
            Key = "RoleId",
            Properties = ["RoleName"]
        };

        var role = new TestRole { RoleId = "role-admin", RoleName = "Administrator" };

        // Act
        lookupOptions.Resolve(typeof(TestRole), typeof(TestUserRole));
        var keyValue = lookupOptions.GetKeyValue(role);

        // Assert
        keyValue.Should().Be("role-admin");
    }

    [Fact]
    public void AppSettings_Lookup_AfterResolve_ShouldGetPropertyValues()
    {
        // Arrange
        var lookupOptions = new LookupOptions
        {
            ForeignKey = "RoleId",
            Key = "RoleId",
            Properties = ["RoleName", "Description"]
        };

        var role = new TestRole
        {
            RoleId = "role-admin",
            RoleName = "Administrator",
            Description = "Full access"
        };

        // Act
        lookupOptions.Resolve(typeof(TestRole), typeof(TestUserRole));
        var propertyValues = lookupOptions.GetPropertyValues(role);

        // Assert
        propertyValues["RoleName"].Should().Be("Administrator");
        propertyValues["Description"].Should().Be("Full access");
    }

    [Fact]
    public void AppSettings_GetRelatedEntity_ShouldCacheResult()
    {
        // Arrange
        var options = new AuditaXOptions();

        options.Entities["TestUser"] = new EntityOptions
        {
            Key = "UserId",
            RelatedEntities = new Dictionary<string, RelatedEntityOptions>
            {
                ["TestUserRole"] = new RelatedEntityOptions
                {
                    ParentKey = "UserId"
                }
            }
        };

        // Act
        var first = options.GetRelatedEntity(typeof(TestUserRole));
        var second = options.GetRelatedEntity(typeof(TestUserRole));

        // Assert - Should return same cached instance
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void GetUnresolvedLookupNames_ShouldReturnOnlyUnresolved()
    {
        // Arrange
        var relatedOptions = new RelatedEntityOptions
        {
            EntityType = typeof(TestUserRole),
            ParentKey = "UserId",
            Lookups = new Dictionary<string, LookupOptions>
            {
                ["Role"] = new LookupOptions
                {
                    ForeignKey = "RoleId",
                    Key = "RoleId",
                    Properties = ["RoleName"]
                },
                ["Department"] = new LookupOptions
                {
                    ForeignKey = "DepartmentId",
                    Key = "DepartmentId",
                    Properties = ["DepartmentName"]
                }
            }
        };

        // Resolve only Role
        relatedOptions.Lookups["Role"].Resolve(typeof(TestRole), typeof(TestUserRole));

        // Act
        var unresolved = relatedOptions.GetUnresolvedLookupNames().ToList();

        // Assert
        unresolved.Should().HaveCount(1);
        unresolved.Should().Contain("Department");
        unresolved.Should().NotContain("Role");
    }

    // ══════════════════════════════════════════════════════════
    // Edge Cases
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void RelatedEntity_WithoutLookups_HasLookupsShouldBeFalse()
    {
        // Arrange
        var options = new AuditaXOptions();
        options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .WithRelatedEntity<TestUserRole>("UserRoles")
                .WithParentKey(ur => ur.UserId)
                .Properties("RoleId"); // No lookups

        // Act
        var relatedOptions = options.GetRelatedEntity(typeof(TestUserRole));

        // Assert
        relatedOptions!.HasLookups.Should().BeFalse();
        relatedOptions.HasUnresolvedLookups.Should().BeFalse();
    }

    [Fact]
    public void Lookup_Resolve_ShouldNotResolveAgainIfAlreadyResolved()
    {
        // Arrange
        var lookupOptions = new LookupOptions
        {
            ForeignKey = "RoleId",
            Key = "RoleId",
            Properties = ["RoleName"]
        };

        // Act - Resolve twice
        lookupOptions.Resolve(typeof(TestRole), typeof(TestUserRole));
        var firstEntityType = lookupOptions.EntityType;

        lookupOptions.Resolve(typeof(TestDepartment), typeof(TestUserRole)); // Try to resolve with different type
        var secondEntityType = lookupOptions.EntityType;

        // Assert - Should keep first resolution
        firstEntityType.Should().Be(typeof(TestRole));
        secondEntityType.Should().Be(typeof(TestRole)); // Not changed
    }

    [Fact]
    public void GetLookup_ShouldReturnNullForNonExistentLookup()
    {
        // Arrange
        var relatedOptions = new RelatedEntityOptions
        {
            ParentKey = "UserId",
            Lookups = new Dictionary<string, LookupOptions>
            {
                ["Role"] = new LookupOptions()
            }
        };

        // Act
        var lookup = relatedOptions.GetLookup("NonExistent");

        // Assert
        lookup.Should().BeNull();
    }

    [Fact]
    public void GetPropertyValues_WithEmptyProperties_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var lookupOptions = new LookupOptions
        {
            ForeignKey = "RoleId",
            Key = "RoleId",
            Properties = [] // Empty
        };

        lookupOptions.Resolve(typeof(TestRole), typeof(TestUserRole));

        var role = new TestRole { RoleId = "role-1", RoleName = "Admin" };

        // Act
        var values = lookupOptions.GetPropertyValues(role);

        // Assert
        values.Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════
    // Test Entities
    // ══════════════════════════════════════════════════════════

    private class TestUser
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private class TestRole
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    private class TestUserRole
    {
        public string UserId { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
    }

    private class TestUserRoleExtended
    {
        public string UserId { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
        public string DepartmentId { get; set; } = string.Empty;
    }

    private class TestDepartment
    {
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
    }
}
