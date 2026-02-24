using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using AuditaX.Configuration;
using AuditaX.Entities;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;
using AuditaX.EntityFramework.Interceptors;
using AuditaX.EntityFramework.Tests.TestEntities;

namespace AuditaX.EntityFramework.Tests.Interceptors;

public class AuditSaveChangesInterceptorTests : IDisposable
{
    private readonly AuditaXOptions _options;
    private readonly Mock<IChangeLogService> _changeLogServiceMock;
    private readonly Mock<IAuditUserProvider> _userProviderMock;
    private readonly AuditSaveChangesInterceptor _interceptor;
    private readonly InterceptorTestDbContext _dbContext;

    public AuditSaveChangesInterceptorTests()
    {
        _options = new AuditaXOptions();
        _changeLogServiceMock = new Mock<IChangeLogService>();
        _userProviderMock = new Mock<IAuditUserProvider>();
        _userProviderMock.Setup(u => u.GetCurrentUser()).Returns("TestUser");

        _changeLogServiceMock
            .Setup(c => c.CreateEntry(It.IsAny<string?>(), It.IsAny<string>()))
            .Returns("<AuditLog><Entry Action=\"Created\" /></AuditLog>");
        _changeLogServiceMock
            .Setup(c => c.UpdateEntry(It.IsAny<string?>(), It.IsAny<List<FieldChange>>(), It.IsAny<string>()))
            .Returns("<AuditLog><Entry Action=\"Updated\" /></AuditLog>");
        _changeLogServiceMock
            .Setup(c => c.DeleteEntry(It.IsAny<string?>(), It.IsAny<string>()))
            .Returns("<AuditLog><Entry Action=\"Deleted\" /></AuditLog>");
        _changeLogServiceMock
            .Setup(c => c.RelatedEntry(It.IsAny<string?>(), It.IsAny<AuditAction>(), It.IsAny<string>(), It.IsAny<List<FieldChange>>(), It.IsAny<string>()))
            .Returns("<AuditLog><Entry Action=\"Added\" /></AuditLog>");
        _changeLogServiceMock
            .Setup(c => c.HasChanged(It.IsAny<object?>(), It.IsAny<object?>()))
            .Returns(true);
        _changeLogServiceMock
            .Setup(c => c.ConvertToString(It.IsAny<object?>()))
            .Returns<object?>(v => v?.ToString());

        _interceptor = new AuditSaveChangesInterceptor(
            _options,
            _changeLogServiceMock.Object,
            _userProviderMock.Object);

        var dbContextOptions = new DbContextOptionsBuilder<InterceptorTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(_interceptor)
            .Options;

        _dbContext = new InterceptorTestDbContext(dbContextOptions);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private void ConfigureProductEntity()
    {
        _options.ConfigureEntity<Product>("Product")
            .WithKey(p => p.Id)
            .Properties("Name", "Price", "Stock", "IsActive");
    }

    #region Entity Changes - Added

    [Fact]
    public async Task SavingChangesAsync_AddedEntity_ShouldCreateAuditLogAfterSave()
    {
        ConfigureProductEntity();

        var product = new Product { Name = "Widget", Price = 10.00m };
        _dbContext.Products.Add(product);

        await _dbContext.SaveChangesAsync();

        var auditLogs = await _dbContext.Set<AuditLog>().ToListAsync();
        auditLogs.Should().HaveCount(1);
        auditLogs[0].SourceName.Should().Be("Product");
    }

    [Fact]
    public async Task SavingChangesAsync_AddedEntity_ShouldUseEntityKey()
    {
        ConfigureProductEntity();

        var product = new Product { Name = "Widget", Price = 10.00m };
        _dbContext.Products.Add(product);

        await _dbContext.SaveChangesAsync();

        var auditLogs = await _dbContext.Set<AuditLog>().ToListAsync();
        auditLogs.Should().HaveCount(1);
        auditLogs[0].SourceKey.Should().Be(product.Id.ToString());
    }

    #endregion

    #region Entity Changes - Modified

    [Fact]
    public async Task SavingChangesAsync_ModifiedEntity_ShouldCaptureChanges()
    {
        ConfigureProductEntity();

        // First create the entity with an audit log
        var product = new Product { Name = "Widget", Price = 10.00m };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // Now modify it
        product.Price = 20.00m;
        await _dbContext.SaveChangesAsync();

        _changeLogServiceMock.Verify(
            c => c.UpdateEntry(It.IsAny<string?>(), It.IsAny<List<FieldChange>>(), "TestUser"),
            Times.Once);
    }

    [Fact]
    public async Task SavingChangesAsync_ModifiedEntity_NoTrackedChanges_ShouldNotCreateUpdateEntry()
    {
        ConfigureProductEntity();

        var product = new Product { Name = "Widget", Price = 10.00m };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // Modify a property that's NOT in the audit config
        product.Description = "A nice widget";
        await _dbContext.SaveChangesAsync();

        // UpdateEntry should not be called since Description is not in Properties
        _changeLogServiceMock.Verify(
            c => c.UpdateEntry(It.IsAny<string?>(), It.IsAny<List<FieldChange>>(), It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region Entity Changes - Deleted

    [Fact]
    public async Task SavingChangesAsync_DeletedEntity_ShouldLogDeleteEvent()
    {
        ConfigureProductEntity();

        var product = new Product { Name = "Widget", Price = 10.00m };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();

        _changeLogServiceMock.Verify(
            c => c.DeleteEntry(It.IsAny<string?>(), "TestUser"),
            Times.Once);
    }

    #endregion

    #region Unconfigured Entities

    [Fact]
    public async Task SavingChangesAsync_UnconfiguredEntity_ShouldBeIgnored()
    {
        // Don't configure Product entity
        var product = new Product { Name = "Widget", Price = 10.00m };
        _dbContext.Products.Add(product);

        await _dbContext.SaveChangesAsync();

        var auditLogs = await _dbContext.Set<AuditLog>().ToListAsync();
        auditLogs.Should().BeEmpty();
    }

    #endregion

    #region User Provider

    [Fact]
    public async Task SavingChangesAsync_ShouldUseUserProvider()
    {
        ConfigureProductEntity();

        var product = new Product { Name = "Widget", Price = 10.00m };
        _dbContext.Products.Add(product);

        await _dbContext.SaveChangesAsync();

        _userProviderMock.Verify(u => u.GetCurrentUser(), Times.AtLeastOnce);
    }

    #endregion

    #region Audit Log Creation - New vs Existing

    [Fact]
    public async Task SavingChangesAsync_NewAuditLog_ShouldCreateNewRecord()
    {
        ConfigureProductEntity();

        var product = new Product { Name = "Widget", Price = 10.00m };
        _dbContext.Products.Add(product);

        await _dbContext.SaveChangesAsync();

        var auditLogs = await _dbContext.Set<AuditLog>().ToListAsync();
        auditLogs.Should().HaveCount(1);
        auditLogs[0].AuditLogXml.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SavingChangesAsync_ExistingAuditLog_ShouldUpdateExisting()
    {
        ConfigureProductEntity();

        var product = new Product { Name = "Widget", Price = 10.00m };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var auditLogCount = await _dbContext.Set<AuditLog>().CountAsync();
        auditLogCount.Should().Be(1);

        // Modify the product
        product.Price = 25.00m;
        await _dbContext.SaveChangesAsync();

        // Should still be 1 audit log, not 2
        auditLogCount = await _dbContext.Set<AuditLog>().CountAsync();
        auditLogCount.Should().Be(1);
    }

    #endregion

    #region Re-created Entity (v1.0.4 fix)

    [Fact]
    public async Task SavingChangesAsync_ReCreatedEntity_ShouldAppendToExistingAuditLog()
    {
        ConfigureProductEntity();

        // Create original entity
        var product = new Product { Name = "Widget", Price = 10.00m };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();
        var originalId = product.Id;

        // Delete it
        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();

        // Re-create with same key
        var newProduct = new Product { Id = originalId, Name = "Widget Reborn", Price = 15.00m };
        _dbContext.Products.Add(newProduct);
        await _dbContext.SaveChangesAsync();

        // Should still have exactly one audit log for this entity
        var auditLogs = await _dbContext.Set<AuditLog>()
            .Where(a => a.SourceName == "Product" && a.SourceKey == originalId.ToString())
            .ToListAsync();
        auditLogs.Should().HaveCount(1);
    }

    #endregion

    #region Sync SaveChanges

    [Fact]
    public void SavingChanges_AddedEntity_ShouldCreateAuditLog()
    {
        ConfigureProductEntity();

        var product = new Product { Name = "Sync Widget", Price = 5.00m };
        _dbContext.Products.Add(product);

        _dbContext.SaveChanges();

        var auditLogs = _dbContext.Set<AuditLog>().ToList();
        auditLogs.Should().HaveCount(1);
        auditLogs[0].SourceName.Should().Be("Product");
    }

    #endregion

    #region Multiple Entities

    [Fact]
    public async Task SavingChangesAsync_MultipleEntities_ShouldCreateSeparateAuditLogs()
    {
        ConfigureProductEntity();

        var product1 = new Product { Name = "Widget A", Price = 10.00m };
        var product2 = new Product { Name = "Widget B", Price = 20.00m };
        _dbContext.Products.Add(product1);
        _dbContext.Products.Add(product2);

        await _dbContext.SaveChangesAsync();

        var auditLogs = await _dbContext.Set<AuditLog>().ToListAsync();
        auditLogs.Should().HaveCount(2);
    }

    #endregion

    #region Field Changes Detection

    [Fact]
    public async Task SavingChangesAsync_ModifiedTrackedProperty_ShouldIncludeInChanges()
    {
        ConfigureProductEntity();
        _changeLogServiceMock
            .Setup(c => c.HasChanged(It.IsAny<object?>(), It.IsAny<object?>()))
            .Returns(true);

        var product = new Product { Name = "Widget", Price = 10.00m };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        product.Name = "Super Widget";
        await _dbContext.SaveChangesAsync();

        _changeLogServiceMock.Verify(
            c => c.UpdateEntry(
                It.IsAny<string?>(),
                It.Is<List<FieldChange>>(changes => changes.Any(fc => fc.Name == "Name")),
                "TestUser"),
            Times.Once);
    }

    #endregion

    /// <summary>
    /// Test DbContext with AuditLog entity configured for InMemory provider.
    /// </summary>
    private class InterceptorTestDbContext : DbContext
    {
        public InterceptorTestDbContext(DbContextOptions<InterceptorTestDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Price).HasPrecision(18, 2);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.SourceName).HasMaxLength(64).IsRequired();
                entity.Property(e => e.SourceKey).HasMaxLength(64).IsRequired();
                entity.Property(e => e.AuditLogXml).IsRequired();
                entity.HasIndex(e => new { e.SourceName, e.SourceKey }).IsUnique();
            });
        }
    }
}
