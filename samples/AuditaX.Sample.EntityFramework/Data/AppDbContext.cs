using Microsoft.EntityFrameworkCore;
using AuditaX.Samples.Common.Entities;

namespace AuditaX.Sample.EntityFramework.Data;

/// <summary>
/// Entity Framework DbContext for the sample.
/// Supports both SQL Server and PostgreSQL.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductTag> ProductTags => Set<ProductTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Detect if using PostgreSQL for snake_case naming
        var isPostgreSql = Database.ProviderName?.Contains("Npgsql") == true;

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            if (isPostgreSql)
            {
                entity.ToTable("products");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.Price).HasColumnName("price");
                entity.Property(e => e.Stock).HasColumnName("stock");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
            }

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });

        // ProductTag configuration
        modelBuilder.Entity<ProductTag>(entity =>
        {
            if (isPostgreSql)
            {
                entity.ToTable("product_tags");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ProductId).HasColumnName("product_id");
                entity.Property(e => e.Tag).HasColumnName("tag");
            }

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tag).IsRequired().HasMaxLength(100);

            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
