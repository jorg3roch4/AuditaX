using Moq;
using AuditaX.Configuration;
using AuditaX.Entities;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;
using AuditaX.Services;

namespace AuditaX.Tests.Services;

public class AuditServiceTests
{
    private readonly Mock<IAuditRepository> _repositoryMock;
    private readonly Mock<IChangeLogService> _changeLogServiceMock;
    private readonly Mock<IAuditUserProvider> _userProviderMock;
    private readonly AuditaXOptions _options;
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        _repositoryMock = new Mock<IAuditRepository>();
        _changeLogServiceMock = new Mock<IChangeLogService>();
        _userProviderMock = new Mock<IAuditUserProvider>();
        _options = new AuditaXOptions();

        _userProviderMock.Setup(u => u.GetCurrentUser()).Returns("TestUser");

        _service = new AuditService(
            _repositoryMock.Object,
            _changeLogServiceMock.Object,
            _userProviderMock.Object,
            _options);
    }

    #region LogCreateAsync

    [Fact]
    public async Task LogCreateAsync_NewEntity_ShouldCreateAuditLogAndSave()
    {
        _repositoryMock
            .Setup(r => r.GetByEntityTrackingAsync("Product", "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuditLog?)null);
        _changeLogServiceMock
            .Setup(c => c.CreateEntry(string.Empty, "TestUser"))
            .Returns("<AuditLog><Entry /></AuditLog>");

        await _service.LogCreateAsync("Product", "1");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogCreateAsync_ExistingEntity_ShouldUpdateExistingAuditLog()
    {
        var existingLog = new AuditLog { SourceName = "Product", SourceKey = "1", AuditLogXml = "<old />" };
        _repositoryMock
            .Setup(r => r.GetByEntityTrackingAsync("Product", "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLog);
        _changeLogServiceMock
            .Setup(c => c.CreateEntry("<old />", "TestUser"))
            .Returns("<new />");

        await _service.LogCreateAsync("Product", "1");

        existingLog.AuditLogXml.Should().Be("<new />");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogCreateAsync_WithExplicitUser_ShouldUseProvidedUser()
    {
        _repositoryMock
            .Setup(r => r.GetByEntityTrackingAsync("Product", "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuditLog?)null);
        _changeLogServiceMock
            .Setup(c => c.CreateEntry(string.Empty, "admin"))
            .Returns("<xml />");

        await _service.LogCreateAsync("Product", "1", "admin");

        _changeLogServiceMock.Verify(c => c.CreateEntry(string.Empty, "admin"), Times.Once);
        _userProviderMock.Verify(u => u.GetCurrentUser(), Times.Never);
    }

    #endregion

    #region LogUpdateAsync

    [Fact]
    public async Task LogUpdateAsync_WithChanges_ShouldUpdateAndSave()
    {
        var existingLog = new AuditLog { SourceName = "Product", SourceKey = "1", AuditLogXml = "<old />" };
        var changes = new List<FieldChange>
        {
            new() { Name = "Price", Before = "10", After = "20" }
        };

        _repositoryMock
            .Setup(r => r.GetByEntityTrackingAsync("Product", "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLog);
        _changeLogServiceMock
            .Setup(c => c.UpdateEntry("<old />", changes, "TestUser"))
            .Returns("<updated />");

        await _service.LogUpdateAsync("Product", "1", changes);

        existingLog.AuditLogXml.Should().Be("<updated />");
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogUpdateAsync_EmptyChanges_ShouldReturnEarlyWithoutSaving()
    {
        var changes = new List<FieldChange>();

        await _service.LogUpdateAsync("Product", "1", changes);

        _repositoryMock.Verify(r => r.GetByEntityTrackingAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LogUpdateAsync_WithExplicitUser_ShouldUseProvidedUser()
    {
        var existingLog = new AuditLog { SourceName = "Product", SourceKey = "1", AuditLogXml = "<old />" };
        var changes = new List<FieldChange> { new() { Name = "Name", Before = "A", After = "B" } };

        _repositoryMock
            .Setup(r => r.GetByEntityTrackingAsync("Product", "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLog);
        _changeLogServiceMock
            .Setup(c => c.UpdateEntry("<old />", changes, "admin"))
            .Returns("<updated />");

        await _service.LogUpdateAsync("Product", "1", changes, "admin");

        _changeLogServiceMock.Verify(c => c.UpdateEntry("<old />", changes, "admin"), Times.Once);
        _userProviderMock.Verify(u => u.GetCurrentUser(), Times.Never);
    }

    #endregion

    #region LogDeleteAsync

    [Fact]
    public async Task LogDeleteAsync_ShouldDeleteAndSave()
    {
        var existingLog = new AuditLog { SourceName = "Product", SourceKey = "1", AuditLogXml = "<old />" };

        _repositoryMock
            .Setup(r => r.GetByEntityTrackingAsync("Product", "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLog);
        _changeLogServiceMock
            .Setup(c => c.DeleteEntry("<old />", "TestUser"))
            .Returns("<deleted />");

        await _service.LogDeleteAsync("Product", "1");

        existingLog.AuditLogXml.Should().Be("<deleted />");
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region LogRelatedAsync

    [Fact]
    public async Task LogRelatedAsync_ShouldAddRelatedEntryAndSave()
    {
        var existingLog = new AuditLog { SourceName = "Product", SourceKey = "1", AuditLogXml = "<old />" };
        var fields = new List<FieldChange> { new() { Name = "TagName", Value = "Electronics" } };

        _repositoryMock
            .Setup(r => r.GetByEntityTrackingAsync("Product", "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLog);
        _changeLogServiceMock
            .Setup(c => c.RelatedEntry("<old />", AuditAction.Added, "Tags", fields, "TestUser"))
            .Returns("<related />");

        await _service.LogRelatedAsync("Product", "1", AuditAction.Added, "Tags", fields);

        existingLog.AuditLogXml.Should().Be("<related />");
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogRelatedAsync_WithExplicitUser_ShouldUseProvidedUser()
    {
        var existingLog = new AuditLog { SourceName = "Product", SourceKey = "1", AuditLogXml = "<old />" };
        var fields = new List<FieldChange> { new() { Name = "TagName", Value = "Sale" } };

        _repositoryMock
            .Setup(r => r.GetByEntityTrackingAsync("Product", "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLog);
        _changeLogServiceMock
            .Setup(c => c.RelatedEntry("<old />", AuditAction.Removed, "Tags", fields, "admin"))
            .Returns("<related />");

        await _service.LogRelatedAsync("Product", "1", AuditAction.Removed, "Tags", fields, "admin");

        _userProviderMock.Verify(u => u.GetCurrentUser(), Times.Never);
    }

    #endregion

    #region GetAuditHistoryAsync

    [Fact]
    public async Task GetAuditHistoryAsync_Found_ShouldParseAndReturn()
    {
        var existingLog = new AuditLog { SourceName = "Product", SourceKey = "1", AuditLogXml = "<xml />" };
        var entries = new List<AuditLogEntry>
        {
            new() { Action = AuditAction.Created, User = "admin" }
        };

        _repositoryMock
            .Setup(r => r.GetByEntityAsync("Product", "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLog);
        _changeLogServiceMock
            .Setup(c => c.ParseAuditLog("<xml />"))
            .Returns(entries);

        var result = await _service.GetAuditHistoryAsync("Product", "1");

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].Action.Should().Be(AuditAction.Created);
    }

    [Fact]
    public async Task GetAuditHistoryAsync_NotFound_ShouldReturnNull()
    {
        _repositoryMock
            .Setup(r => r.GetByEntityAsync("Product", "999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuditLog?)null);

        var result = await _service.GetAuditHistoryAsync("Product", "999");

        result.Should().BeNull();
    }

    #endregion

    #region User Provider Interaction

    [Fact]
    public async Task LogCreateAsync_WithoutExplicitUser_ShouldUseUserProvider()
    {
        _repositoryMock
            .Setup(r => r.GetByEntityTrackingAsync("Product", "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuditLog?)null);
        _changeLogServiceMock
            .Setup(c => c.CreateEntry(string.Empty, "TestUser"))
            .Returns("<xml />");

        await _service.LogCreateAsync("Product", "1");

        _userProviderMock.Verify(u => u.GetCurrentUser(), Times.Once);
    }

    [Fact]
    public async Task LogDeleteAsync_WithExplicitUser_ShouldUseProvidedUser()
    {
        var existingLog = new AuditLog { SourceName = "Product", SourceKey = "1", AuditLogXml = "<old />" };

        _repositoryMock
            .Setup(r => r.GetByEntityTrackingAsync("Product", "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLog);
        _changeLogServiceMock
            .Setup(c => c.DeleteEntry("<old />", "admin"))
            .Returns("<deleted />");

        await _service.LogDeleteAsync("Product", "1", "admin");

        _userProviderMock.Verify(u => u.GetCurrentUser(), Times.Never);
    }

    #endregion
}
