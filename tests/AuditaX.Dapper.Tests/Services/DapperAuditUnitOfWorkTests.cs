using AuditaX.Configuration;
using AuditaX.Dapper.Interfaces;
using AuditaX.Dapper.Services;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;
using Moq;

namespace AuditaX.Dapper.Tests.Services;

public class DapperAuditUnitOfWorkTests
{
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IChangeLogService> _changeLogServiceMock;
    private readonly Mock<IAuditUserProvider> _userProviderMock;
    private readonly AuditaXOptions _options;
    private readonly IAuditUnitOfWork _unitOfWork;

    public DapperAuditUnitOfWorkTests()
    {
        _auditServiceMock = new Mock<IAuditService>();
        _changeLogServiceMock = new Mock<IChangeLogService>();
        _userProviderMock = new Mock<IAuditUserProvider>();
        _options = new AuditaXOptions();

        _userProviderMock.Setup(u => u.GetCurrentUser()).Returns("test@example.com");

        _unitOfWork = new DapperAuditUnitOfWork(
            _auditServiceMock.Object,
            _changeLogServiceMock.Object,
            _options,
            _userProviderMock.Object);
    }

    #region LogRelatedAddedAsync Tests

    [Fact]
    public async Task LogRelatedAddedAsync_ShouldCallAuditService_WithCorrectParameters()
    {
        // Arrange
        ConfigureProductWithTags();

        var product = new TestProduct { Id = 1, Name = "Test Product" };
        var tag = new TestProductTag { ProductId = 1, TagName = "Electronics" };

        // Act
        await _unitOfWork.LogRelatedAddedAsync(product, tag);

        // Assert
        _auditServiceMock.Verify(s => s.LogRelatedAsync(
            "Product",
            "1",
            AuditAction.Added,
            "Tags",
            It.Is<List<FieldChange>>(fields =>
                fields.Count == 1 &&
                fields[0].Name == "TagName" &&
                fields[0].Value == "Electronics"),
            "test@example.com"),
            Times.Once);
    }

    [Fact]
    public async Task LogRelatedAddedAsync_WithoutRelatedConfig_ShouldUseTypeName()
    {
        // Arrange
        ConfigureProductOnly();

        var product = new TestProduct { Id = 1, Name = "Test Product" };
        var tag = new TestProductTag { ProductId = 1, TagName = "Electronics" };

        // Act
        await _unitOfWork.LogRelatedAddedAsync(product, tag);

        // Assert
        _auditServiceMock.Verify(s => s.LogRelatedAsync(
            "Product",
            "1",
            AuditAction.Added,
            "TestProductTag",  // Falls back to type name
            It.Is<List<FieldChange>>(fields => fields.Count == 0),
            "test@example.com"),
            Times.Once);
    }

    #endregion

    #region LogRelatedUpdatedAsync Tests

    [Fact]
    public async Task LogRelatedUpdatedAsync_ShouldCallAuditService_WithChangedFields()
    {
        // Arrange
        ConfigureProductWithTags();

        var product = new TestProduct { Id = 1, Name = "Test Product" };
        var originalTag = new TestProductTag { ProductId = 1, TagName = "Electronics" };
        var modifiedTag = new TestProductTag { ProductId = 1, TagName = "Gaming" };

        _changeLogServiceMock.Setup(c => c.HasChanged("Electronics", "Gaming")).Returns(true);
        _changeLogServiceMock.Setup(c => c.ConvertToString("Electronics")).Returns("Electronics");
        _changeLogServiceMock.Setup(c => c.ConvertToString("Gaming")).Returns("Gaming");

        // Act
        await _unitOfWork.LogRelatedUpdatedAsync(product, originalTag, modifiedTag);

        // Assert
        _auditServiceMock.Verify(s => s.LogRelatedAsync(
            "Product",
            "1",
            AuditAction.Updated,
            "Tags",
            It.Is<List<FieldChange>>(fields =>
                fields.Count == 1 &&
                fields[0].Name == "TagName" &&
                fields[0].Before == "Electronics" &&
                fields[0].After == "Gaming"),
            "test@example.com"),
            Times.Once);
    }

    [Fact]
    public async Task LogRelatedUpdatedAsync_WithNoChanges_ShouldNotCallAuditService()
    {
        // Arrange
        ConfigureProductWithTags();

        var product = new TestProduct { Id = 1, Name = "Test Product" };
        var originalTag = new TestProductTag { ProductId = 1, TagName = "Electronics" };
        var modifiedTag = new TestProductTag { ProductId = 1, TagName = "Electronics" };

        _changeLogServiceMock.Setup(c => c.HasChanged("Electronics", "Electronics")).Returns(false);

        // Act
        await _unitOfWork.LogRelatedUpdatedAsync(product, originalTag, modifiedTag);

        // Assert
        _auditServiceMock.Verify(s => s.LogRelatedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<AuditAction>(),
            It.IsAny<string>(),
            It.IsAny<List<FieldChange>>(),
            It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task LogRelatedUpdatedAsync_WithoutRelatedConfig_ShouldNotCallAuditService()
    {
        // Arrange
        ConfigureProductOnly();

        var product = new TestProduct { Id = 1, Name = "Test Product" };
        var originalTag = new TestProductTag { ProductId = 1, TagName = "Electronics" };
        var modifiedTag = new TestProductTag { ProductId = 1, TagName = "Gaming" };

        // Act
        await _unitOfWork.LogRelatedUpdatedAsync(product, originalTag, modifiedTag);

        // Assert - No changes detected because no config means empty properties list
        _auditServiceMock.Verify(s => s.LogRelatedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<AuditAction>(),
            It.IsAny<string>(),
            It.IsAny<List<FieldChange>>(),
            It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region LogRelatedRemovedAsync Tests

    [Fact]
    public async Task LogRelatedRemovedAsync_ShouldCallAuditService_WithCorrectParameters()
    {
        // Arrange
        ConfigureProductWithTags();

        var product = new TestProduct { Id = 1, Name = "Test Product" };
        var tag = new TestProductTag { ProductId = 1, TagName = "Electronics" };

        // Act
        await _unitOfWork.LogRelatedRemovedAsync(product, tag);

        // Assert
        _auditServiceMock.Verify(s => s.LogRelatedAsync(
            "Product",
            "1",
            AuditAction.Removed,
            "Tags",
            It.Is<List<FieldChange>>(fields =>
                fields.Count == 1 &&
                fields[0].Name == "TagName" &&
                fields[0].Value == "Electronics"),
            "test@example.com"),
            Times.Once);
    }

    [Fact]
    public async Task LogRelatedRemovedAsync_WithoutRelatedConfig_ShouldUseTypeName()
    {
        // Arrange
        ConfigureProductOnly();

        var product = new TestProduct { Id = 1, Name = "Test Product" };
        var tag = new TestProductTag { ProductId = 1, TagName = "Electronics" };

        // Act
        await _unitOfWork.LogRelatedRemovedAsync(product, tag);

        // Assert
        _auditServiceMock.Verify(s => s.LogRelatedAsync(
            "Product",
            "1",
            AuditAction.Removed,
            "TestProductTag",  // Falls back to type name
            It.Is<List<FieldChange>>(fields => fields.Count == 0),
            "test@example.com"),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task LogRelatedAddedAsync_WithUnconfiguredParent_ShouldThrow()
    {
        // Arrange - No configuration
        var product = new TestProduct { Id = 1, Name = "Test Product" };
        var tag = new TestProductTag { ProductId = 1, TagName = "Electronics" };

        // Act & Assert
        var action = async () => await _unitOfWork.LogRelatedAddedAsync(product, tag);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TestProduct*not configured*");
    }

    [Fact]
    public async Task LogRelatedRemovedAsync_WithUnconfiguredParent_ShouldThrow()
    {
        // Arrange - No configuration
        var product = new TestProduct { Id = 1, Name = "Test Product" };
        var tag = new TestProductTag { ProductId = 1, TagName = "Electronics" };

        // Act & Assert
        var action = async () => await _unitOfWork.LogRelatedRemovedAsync(product, tag);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TestProduct*not configured*");
    }

    [Fact]
    public async Task LogRelatedUpdatedAsync_WithUnconfiguredParent_ShouldThrow()
    {
        // Arrange - No configuration
        var product = new TestProduct { Id = 1, Name = "Test Product" };
        var originalTag = new TestProductTag { ProductId = 1, TagName = "Electronics" };
        var modifiedTag = new TestProductTag { ProductId = 1, TagName = "Gaming" };

        // Act & Assert
        var action = async () => await _unitOfWork.LogRelatedUpdatedAsync(product, originalTag, modifiedTag);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TestProduct*not configured*");
    }

    #endregion

    #region LogRelatedAddedAsync with Lookups Tests

    [Fact]
    public async Task LogRelatedAddedAsync_WithLookup_ShouldCapturePropertiesFromLookupEntity()
    {
        // Arrange
        ConfigureUserWithRoleLookup();

        var user = new TestUser { UserId = "user-123", UserName = "john.doe" };
        var userRole = new TestUserRole { UserId = "user-123", RoleId = "role-456" };
        var role = new TestRole { RoleId = "role-456", RoleName = "Administrator" };

        // Act
        await _unitOfWork.LogRelatedAddedAsync(user, userRole, role);

        // Assert
        _auditServiceMock.Verify(s => s.LogRelatedAsync(
            "User",
            "user-123",
            AuditAction.Added,
            "UserRoles",
            It.Is<List<FieldChange>>(fields =>
                fields.Count == 1 &&
                fields[0].Name == "RoleName" &&
                fields[0].Value == "Administrator"),
            "test@example.com"),
            Times.Once);
    }

    [Fact]
    public async Task LogRelatedAddedAsync_WithMultipleLookups_ShouldCaptureAllProperties()
    {
        // Arrange
        ConfigureUserWithMultipleLookups();

        var user = new TestUser { UserId = "user-123", UserName = "john.doe" };
        var userRole = new TestUserRole { UserId = "user-123", RoleId = "role-456", DepartmentId = "dept-789" };
        var role = new TestRole { RoleId = "role-456", RoleName = "Administrator" };
        var department = new TestDepartment { DepartmentId = "dept-789", DepartmentName = "IT" };

        // Act
        await _unitOfWork.LogRelatedAddedAsync(user, userRole, role, department);

        // Assert
        _auditServiceMock.Verify(s => s.LogRelatedAsync(
            "User",
            "user-123",
            AuditAction.Added,
            "UserRoles",
            It.Is<List<FieldChange>>(fields =>
                fields.Count == 2 &&
                fields.Any(f => f.Name == "RoleName" && f.Value == "Administrator") &&
                fields.Any(f => f.Name == "DepartmentName" && f.Value == "IT")),
            "test@example.com"),
            Times.Once);
    }

    #endregion

    #region LogRelatedRemovedAsync with Lookups Tests

    [Fact]
    public async Task LogRelatedRemovedAsync_WithLookup_ShouldCapturePropertiesFromLookupEntity()
    {
        // Arrange
        ConfigureUserWithRoleLookup();

        var user = new TestUser { UserId = "user-123", UserName = "john.doe" };
        var userRole = new TestUserRole { UserId = "user-123", RoleId = "role-456" };
        var role = new TestRole { RoleId = "role-456", RoleName = "Administrator" };

        // Act
        await _unitOfWork.LogRelatedRemovedAsync(user, userRole, role);

        // Assert
        _auditServiceMock.Verify(s => s.LogRelatedAsync(
            "User",
            "user-123",
            AuditAction.Removed,
            "UserRoles",
            It.Is<List<FieldChange>>(fields =>
                fields.Count == 1 &&
                fields[0].Name == "RoleName" &&
                fields[0].Value == "Administrator"),
            "test@example.com"),
            Times.Once);
    }

    #endregion

    #region LogRelatedUpdatedAsync with Lookups Tests

    [Fact]
    public async Task LogRelatedUpdatedAsync_WithLookups_ShouldCaptureChangesFromLookupEntities()
    {
        // Arrange
        ConfigureUserWithRoleLookup();

        var user = new TestUser { UserId = "user-123", UserName = "john.doe" };
        var originalUserRole = new TestUserRole { UserId = "user-123", RoleId = "role-old" };
        var modifiedUserRole = new TestUserRole { UserId = "user-123", RoleId = "role-new" };
        var originalRole = new TestRole { RoleId = "role-old", RoleName = "User" };
        var modifiedRole = new TestRole { RoleId = "role-new", RoleName = "Administrator" };

        _changeLogServiceMock.Setup(c => c.HasChanged("User", "Administrator")).Returns(true);

        // Act
        await _unitOfWork.LogRelatedUpdatedAsync(
            user,
            originalUserRole,
            modifiedUserRole,
            new object[] { originalRole },
            new object[] { modifiedRole });

        // Assert
        _auditServiceMock.Verify(s => s.LogRelatedAsync(
            "User",
            "user-123",
            AuditAction.Updated,
            "UserRoles",
            It.Is<List<FieldChange>>(fields =>
                fields.Count == 1 &&
                fields[0].Name == "RoleName" &&
                fields[0].Before == "User" &&
                fields[0].After == "Administrator"),
            "test@example.com"),
            Times.Once);
    }

    [Fact]
    public async Task LogRelatedUpdatedAsync_WithLookups_NoChanges_ShouldNotCallAuditService()
    {
        // Arrange
        ConfigureUserWithRoleLookup();

        var user = new TestUser { UserId = "user-123", UserName = "john.doe" };
        var originalUserRole = new TestUserRole { UserId = "user-123", RoleId = "role-456" };
        var modifiedUserRole = new TestUserRole { UserId = "user-123", RoleId = "role-456" };
        var role = new TestRole { RoleId = "role-456", RoleName = "Administrator" };

        _changeLogServiceMock.Setup(c => c.HasChanged("Administrator", "Administrator")).Returns(false);

        // Act
        await _unitOfWork.LogRelatedUpdatedAsync(
            user,
            originalUserRole,
            modifiedUserRole,
            new object[] { role },
            new object[] { role });

        // Assert
        _auditServiceMock.Verify(s => s.LogRelatedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<AuditAction>(),
            It.IsAny<string>(),
            It.IsAny<List<FieldChange>>(),
            It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region Helper Methods

    private void ConfigureProductWithTags()
    {
        _options.ConfigureEntity<TestProduct>("Product")
            .WithKey(p => p.Id)
            .Properties("Name", "Price")
            .WithRelatedEntity<TestProductTag>("Tags")
                .WithParentKey(t => t.ProductId)
                .Properties("TagName");
    }

    private void ConfigureProductOnly()
    {
        _options.ConfigureEntity<TestProduct>("Product")
            .WithKey(p => p.Id)
            .Properties("Name", "Price");
    }

    private void ConfigureUserWithRoleLookup()
    {
        _options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .Properties("UserName")
            .WithRelatedEntity<TestUserRole>("UserRoles")
                .WithParentKey(ur => ur.UserId)
                .WithLookup<TestRole>("Role")
                    .ForeignKey(ur => ur.RoleId)
                    .Key(r => r.RoleId)
                    .Properties("RoleName");
    }

    private void ConfigureUserWithMultipleLookups()
    {
        _options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .Properties("UserName")
            .WithRelatedEntity<TestUserRole>("UserRoles")
                .WithParentKey(ur => ur.UserId)
                .WithLookup<TestRole>("Role")
                    .ForeignKey(ur => ur.RoleId)
                    .Key(r => r.RoleId)
                    .Properties("RoleName")
                .WithLookup<TestDepartment>("Department")
                    .ForeignKey(ur => ur.DepartmentId)
                    .Key(d => d.DepartmentId)
                    .Properties("DepartmentName");
    }

    #endregion

    #region Test Entities

    private class TestProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    private class TestProductTag
    {
        public int ProductId { get; set; }
        public string TagName { get; set; } = string.Empty;
    }

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
        public string DepartmentId { get; set; } = string.Empty;
    }

    private class TestDepartment
    {
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
    }

    #endregion
}
