using AuditaX.Configuration;
using AuditaX.Dapper.Interfaces;
using AuditaX.Dapper.Services;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;
using Moq;

namespace AuditaX.Dapper.Tests.Services;

/// <summary>
/// Tests covering Identifier vs Key decoupling for the Dapper unit-of-work path.
/// </summary>
public class DapperAuditUnitOfWorkIdentifierTests
{
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IChangeLogService> _changeLogServiceMock;
    private readonly Mock<IAuditUserProvider> _userProviderMock;
    private readonly AuditaXOptions _options;
    private readonly IAuditUnitOfWork _unitOfWork;

    public DapperAuditUnitOfWorkIdentifierTests()
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

    [Fact]
    public async Task LogCreateAsync_With_Identifier_Configured_Uses_Identifier_As_SourceKey()
    {
        // Arrange — WithIdentifier(u => u.UserName) takes precedence over Id
        _options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .WithIdentifier(u => u.UserName)
            .Properties("UserName");

        var user = new TestUser { UserId = "guid-123", UserName = "alice" };

        // Act
        await _unitOfWork.LogCreateAsync(user);

        // Assert
        _auditServiceMock.Verify(
            s => s.LogCreateAsync("User", "alice", "test@example.com"),
            Times.Once);
    }

    [Fact]
    public async Task LogCreateAsync_Without_Identifier_Falls_Back_To_Key()
    {
        // Arrange — back-compat: no WithIdentifier
        _options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .Properties("UserName");

        var user = new TestUser { UserId = "guid-456", UserName = "bob" };

        // Act
        await _unitOfWork.LogCreateAsync(user);

        // Assert
        _auditServiceMock.Verify(
            s => s.LogCreateAsync("User", "guid-456", "test@example.com"),
            Times.Once);
    }

    [Fact]
    public async Task LogUpdateAsync_With_Identifier_Configured_Uses_Identifier_As_SourceKey()
    {
        // Arrange
        _options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .WithIdentifier(u => u.UserName)
            .Properties("UserName");

        _changeLogServiceMock.Setup(c => c.HasChanged(It.IsAny<object?>(), It.IsAny<object?>())).Returns(true);
        _changeLogServiceMock.Setup(c => c.ConvertToString(It.IsAny<object?>())).Returns<object?>(v => v?.ToString());

        var original = new TestUser { UserId = "guid-789", UserName = "alice" };
        var modified = new TestUser { UserId = "guid-789", UserName = "alice2" };

        // Act
        await _unitOfWork.LogUpdateAsync(original, modified);

        // Assert — sourceKey is taken from MODIFIED entity's identifier
        _auditServiceMock.Verify(
            s => s.LogUpdateAsync(
                "User",
                "alice2",
                It.IsAny<List<FieldChange>>(),
                "test@example.com"),
            Times.Once);
    }

    [Fact]
    public async Task LogDeleteAsync_With_Identifier_Configured_Uses_Identifier_As_SourceKey()
    {
        // Arrange
        _options.ConfigureEntity<TestUser>("User")
            .WithKey(u => u.UserId)
            .WithIdentifier(u => u.UserName)
            .Properties("UserName");

        var user = new TestUser { UserId = "guid-abc", UserName = "carol" };

        // Act
        await _unitOfWork.LogDeleteAsync(user);

        // Assert
        _auditServiceMock.Verify(
            s => s.LogDeleteAsync("User", "carol", "test@example.com"),
            Times.Once);
    }

    private class TestUser
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
