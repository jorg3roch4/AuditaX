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
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

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

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            if (isPostgreSql)
            {
                entity.ToTable("users");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.UserName).HasColumnName("user_name");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.PhoneNumber).HasColumnName("phone_number");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
            }

            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).ValueGeneratedNever(); // Client-generated GUID
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
        });

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            if (isPostgreSql)
            {
                entity.ToTable("roles");
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.RoleName).HasColumnName("role_name");
                entity.Property(e => e.Description).HasColumnName("description");
            }

            entity.HasKey(e => e.RoleId);
            entity.Property(e => e.RoleId).ValueGeneratedNever(); // Client-generated GUID
            entity.Property(e => e.RoleName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // UserRole configuration (junction table with composite key)
        modelBuilder.Entity<UserRole>(entity =>
        {
            if (isPostgreSql)
            {
                entity.ToTable("user_roles");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.RoleId).HasColumnName("role_id");
            }

            // Composite primary key
            entity.HasKey(e => new { e.UserId, e.RoleId });

            // Foreign key to User
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to Role
            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
