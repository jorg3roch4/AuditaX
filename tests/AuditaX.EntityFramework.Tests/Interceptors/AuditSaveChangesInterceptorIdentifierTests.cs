using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moq;
using AuditaX.Configuration;
using AuditaX.Entities;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;
using AuditaX.EntityFramework.Interceptors;
using AuditaX.EntityFramework.Tests.TestEntities;

namespace AuditaX.EntityFramework.Tests.Interceptors;

/// <summary>
/// Tests covering the new Identifier vs Key decoupling behavior for the EF interceptor.
/// SourceKey resolution must use <see cref="EntityOptions.GetIdentifier"/> when an identifier
/// is configured, falling back to <see cref="EntityOptions.GetKey"/> otherwise.
/// </summary>
public class AuditSaveChangesInterceptorIdentifierTests : IDisposable
{
    private readonly AuditaXOptions _options;
    private readonly Mock<IChangeLogService> _changeLogServiceMock;
    private readonly Mock<IAuditUserProvider> _userProviderMock;
    private readonly AuditSaveChangesInterceptor _interceptor;
    private readonly IdentifierTestDbContext _dbContext;

    public AuditSaveChangesInterceptorIdentifierTests()
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

        var dbContextOptions = new DbContextOptionsBuilder<IdentifierTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(_interceptor)
            .Options;

        _dbContext = new IdentifierTestDbContext(dbContextOptions);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private void ConfigureParentWithIdentifier()
    {
        _options.ConfigureEntity<ParentProduct>("ParentProduct")
            .WithKey(p => p.Id)
            .WithIdentifier(p => p.Sku)
            .Properties("Name", "Sku");
    }

    private void ConfigureParentWithIdentifierAndChild()
    {
        _options.ConfigureEntity<ParentProduct>("ParentProduct")
            .WithKey(p => p.Id)
            .WithIdentifier(p => p.Sku)
            .Properties("Name", "Sku")
            .WithRelatedEntity<ProductTag>("Tags")
                .WithParentKey(t => t.ProductId)
                .Properties("TagName");
    }

    #region B1 — Parent SourceKey for Added/Modified/Deleted uses Identifier

    [Fact]
    public async Task Added_Parent_With_Identifier_Writes_Identifier_As_SourceKey()
    {
        ConfigureParentWithIdentifier();

        var product = new ParentProduct { Sku = "SKU-001", Name = "Widget" };
        _dbContext.Products.Add(product);

        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var auditLogs = await _dbContext.Set<AuditLog>().ToListAsync(TestContext.Current.CancellationToken);
        auditLogs.Should().HaveCount(1);
        auditLogs[0].SourceKey.Should().Be("SKU-001");
        auditLogs[0].SourceKey.Should().NotBe(product.Id.ToString());
    }

    [Fact]
    public async Task Modified_Parent_With_Identifier_Writes_Identifier_As_SourceKey()
    {
        ConfigureParentWithIdentifier();

        var product = new ParentProduct { Sku = "SKU-002", Name = "Widget" };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        product.Name = "Super Widget";
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var auditLogs = await _dbContext.Set<AuditLog>()
            .Where(a => a.SourceName == "ParentProduct" && a.SourceKey == "SKU-002")
            .ToListAsync(TestContext.Current.CancellationToken);
        auditLogs.Should().HaveCount(1);
    }

    [Fact]
    public async Task Deleted_Parent_With_Identifier_Writes_Identifier_As_SourceKey()
    {
        ConfigureParentWithIdentifier();

        var product = new ParentProduct { Sku = "SKU-003", Name = "Widget" };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var auditLogs = await _dbContext.Set<AuditLog>()
            .Where(a => a.SourceName == "ParentProduct" && a.SourceKey == "SKU-003")
            .ToListAsync(TestContext.Current.CancellationToken);
        auditLogs.Should().HaveCount(1);
    }

    [Fact]
    public async Task Added_Parent_Without_Identifier_Falls_Back_To_Key()
    {
        // Back-compat: no WithIdentifier configured — SourceKey must equal Key
        _options.ConfigureEntity<ParentProduct>("ParentProduct")
            .WithKey(p => p.Id)
            .Properties("Name", "Sku");

        var product = new ParentProduct { Sku = "SKU-NA", Name = "Widget" };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var auditLogs = await _dbContext.Set<AuditLog>().ToListAsync(TestContext.Current.CancellationToken);
        auditLogs.Should().HaveCount(1);
        auditLogs[0].SourceKey.Should().Be(product.Id.ToString());
    }

    #endregion

    #region B3 — Related entity, parent in ChangeTracker

    [Fact]
    public async Task Related_Entity_With_Parent_In_ChangeTracker_Resolves_Identifier()
    {
        ConfigureParentWithIdentifierAndChild();

        var product = new ParentProduct { Sku = "SKU-IN-TRACKER", Name = "Widget" };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Now both parent and child are added in same SaveChanges
        var product2 = new ParentProduct { Sku = "SKU-CHILD-PARENT", Name = "Widget2" };
        _dbContext.Products.Add(product2);
        // Need to save first so we can have Id, then add child in another SaveChanges where parent is still tracked
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tag = new ProductTag { ProductId = product2.Id, TagName = "Hot" };
        _dbContext.Tags.Add(tag);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var auditLogs = await _dbContext.Set<AuditLog>()
            .Where(a => a.SourceName == "ParentProduct" && a.SourceKey == "SKU-CHILD-PARENT")
            .ToListAsync(TestContext.Current.CancellationToken);
        auditLogs.Should().HaveCount(1, "child should consolidate under the parent's identifier-keyed audit log");
    }

    #endregion

    #region B4 — Related entity, parent only in DB (loaded via Find)

    [Fact]
    public async Task Related_Entity_With_Parent_Only_In_DB_Loads_Via_Find_And_Resolves_Identifier()
    {
        ConfigureParentWithIdentifierAndChild();

        // 1. Pre-seed parent in DB
        var product = new ParentProduct { Sku = "SKU-DETACHED", Name = "Widget" };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var parentId = product.Id;

        // 2. Detach parent so only the FK is known when child is added
        _dbContext.Entry(product).State = EntityState.Detached;

        // 3. Add child with FK only
        var tag = new ProductTag { ProductId = parentId, TagName = "Cold" };
        _dbContext.Tags.Add(tag);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 4. The interceptor must call context.Find to load parent then write SKU as SourceKey
        var auditLogs = await _dbContext.Set<AuditLog>()
            .Where(a => a.SourceName == "ParentProduct" && a.SourceKey == "SKU-DETACHED")
            .ToListAsync(TestContext.Current.CancellationToken);
        auditLogs.Should().HaveCount(1);
    }

    #endregion

    #region B5 — Orphan FK falls back to FK value, no crash

    [Fact]
    public async Task Related_Entity_With_Missing_Parent_Falls_Back_To_FK_Value()
    {
        ConfigureParentWithIdentifierAndChild();

        // Add a tag pointing to a non-existent parent
        var orphanFk = 9999;
        var tag = new ProductTag { ProductId = orphanFk, TagName = "Orphan" };
        _dbContext.Tags.Add(tag);

        var act = async () => await _dbContext.SaveChangesAsync();
        await act.Should().NotThrowAsync("auditing must never crash the host save");

        // The audit log must use the raw FK value as SourceKey
        var auditLogs = await _dbContext.Set<AuditLog>()
            .Where(a => a.SourceName == "ParentProduct" && a.SourceKey == orphanFk.ToString())
            .ToListAsync(TestContext.Current.CancellationToken);
        auditLogs.Should().HaveCount(1);
    }

    #endregion

    #region B8 — Multiple children consolidate to one AuditLog row

    [Fact]
    public async Task Multiple_Related_Changes_Same_Parent_Consolidate_Into_One_AuditLog_Row()
    {
        ConfigureParentWithIdentifierAndChild();

        var product = new ParentProduct { Sku = "SKU-CONSOL", Name = "Widget" };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add two tags in same SaveChanges
        var tag1 = new ProductTag { ProductId = product.Id, TagName = "TagA" };
        var tag2 = new ProductTag { ProductId = product.Id, TagName = "TagB" };
        _dbContext.Tags.Add(tag1);
        _dbContext.Tags.Add(tag2);

        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var auditLogs = await _dbContext.Set<AuditLog>()
            .Where(a => a.SourceName == "ParentProduct" && a.SourceKey == "SKU-CONSOL")
            .ToListAsync(TestContext.Current.CancellationToken);
        auditLogs.Should().HaveCount(1, "two related changes should consolidate into one AuditLog row keyed by parent identifier");
    }

    #endregion

    /// <summary>
    /// Test DbContext for parent + related child entities + AuditLog.
    /// </summary>
    private class IdentifierTestDbContext : DbContext
    {
        public IdentifierTestDbContext(DbContextOptions<IdentifierTestDbContext> options) : base(options) { }

        public DbSet<ParentProduct> Products => Set<ParentProduct>();
        public DbSet<ProductTag> Tags => Set<ProductTag>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ParentProduct>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Sku).HasMaxLength(64).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            });

            modelBuilder.Entity<ProductTag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TagName).HasMaxLength(64).IsRequired();
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
