using Microsoft.EntityFrameworkCore;
using AuditaX.Entities;
using AuditaX.EntityFramework.Repositories;

namespace AuditaX.EntityFramework.Tests.Repositories;

public class EfAuditRepositoryTests : IDisposable
{
    private readonly DbContext _dbContext;
    private readonly EfAuditRepository _repository;

    public EfAuditRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TestAuditDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestAuditDbContext(options);
        _repository = new EfAuditRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetByEntityAsync_NotFound_ShouldReturnNull()
    {
        var result = await _repository.GetByEntityAsync("Product", "999", TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEntityAsync_Found_ShouldReturnEntity()
    {
        var auditLog = new AuditLog
        {
            SourceName = "Product",
            SourceKey = "1",
            AuditLogXml = "<AuditLog />"
        };
        _dbContext.Set<AuditLog>().Add(auditLog);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetByEntityAsync("Product", "1", TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.SourceName.Should().Be("Product");
        result.SourceKey.Should().Be("1");
    }

    [Fact]
    public async Task GetByEntityAsync_ShouldReturnUntrackedEntity()
    {
        var auditLog = new AuditLog
        {
            SourceName = "Product",
            SourceKey = "2",
            AuditLogXml = "<AuditLog />"
        };
        _dbContext.Set<AuditLog>().Add(auditLog);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        _dbContext.ChangeTracker.Clear();

        var result = await _repository.GetByEntityAsync("Product", "2", TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        var entry = _dbContext.Entry(result!);
        entry.State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task GetByEntityTrackingAsync_NotFound_ShouldReturnNull()
    {
        var result = await _repository.GetByEntityTrackingAsync("Product", "999", TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEntityTrackingAsync_Found_ShouldReturnTrackedEntity()
    {
        var auditLog = new AuditLog
        {
            SourceName = "Product",
            SourceKey = "3",
            AuditLogXml = "<AuditLog />"
        };
        _dbContext.Set<AuditLog>().Add(auditLog);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        _dbContext.ChangeTracker.Clear();

        var result = await _repository.GetByEntityTrackingAsync("Product", "3", TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        var entry = _dbContext.Entry(result!);
        entry.State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public async Task AddAsync_ShouldAddToContext()
    {
        var auditLog = new AuditLog
        {
            SourceName = "Order",
            SourceKey = "100",
            AuditLogXml = "<AuditLog />"
        };

        await _repository.AddAsync(auditLog, TestContext.Current.CancellationToken);

        var entry = _dbContext.Entry(auditLog);
        entry.State.Should().Be(EntityState.Added);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistChanges()
    {
        var auditLog = new AuditLog
        {
            SourceName = "Order",
            SourceKey = "101",
            AuditLogXml = "<AuditLog />"
        };
        await _repository.AddAsync(auditLog, TestContext.Current.CancellationToken);

        await _repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _dbContext.Set<AuditLog>()
            .FirstOrDefaultAsync(a => a.SourceName == "Order" && a.SourceKey == "101", TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByEntityTrackingAsync_ShouldAllowModification()
    {
        var auditLog = new AuditLog
        {
            SourceName = "Product",
            SourceKey = "4",
            AuditLogXml = "<original />"
        };
        _dbContext.Set<AuditLog>().Add(auditLog);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        _dbContext.ChangeTracker.Clear();

        var tracked = await _repository.GetByEntityTrackingAsync("Product", "4", TestContext.Current.CancellationToken);
        tracked!.AuditLogXml = "<updated />";
        await _repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dbContext.ChangeTracker.Clear();
        var result = await _dbContext.Set<AuditLog>()
            .FirstOrDefaultAsync(a => a.SourceName == "Product" && a.SourceKey == "4", TestContext.Current.CancellationToken);
        result!.AuditLogXml.Should().Be("<updated />");
    }

    [Fact]
    public async Task AddAsync_MultipleLogs_ShouldAllBePersisted()
    {
        await _repository.AddAsync(new AuditLog { SourceName = "A", SourceKey = "1", AuditLogXml = "<xml />" }, TestContext.Current.CancellationToken);
        await _repository.AddAsync(new AuditLog { SourceName = "B", SourceKey = "2", AuditLogXml = "<xml />" }, TestContext.Current.CancellationToken);
        await _repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        var count = await _dbContext.Set<AuditLog>().CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(2);
    }

    /// <summary>
    /// Minimal DbContext for testing EfAuditRepository with InMemory provider.
    /// </summary>
    private class TestAuditDbContext : DbContext
    {
        public TestAuditDbContext(DbContextOptions<TestAuditDbContext> options) : base(options) { }

        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
