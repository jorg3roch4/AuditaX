using Microsoft.EntityFrameworkCore;
using AuditaX.Configuration;
using AuditaX.Entities;
using AuditaX.Enums;
using AuditaX.Interfaces;

namespace AuditaX.EntityFramework.Contexts;

/// <summary>
/// Internal DbContext used by AuditaX for audit log operations.
/// This is used for both standalone scenarios and when sharing a connection with the application's DbContext.
/// </summary>
internal class AuditaXDbContext : DbContext
{
    private readonly AuditaXOptions _auditaXOptions;
    private readonly IDatabaseProvider _databaseProvider;

    public AuditaXDbContext(
        DbContextOptions<AuditaXDbContext> options,
        AuditaXOptions auditaXOptions,
        IDatabaseProvider databaseProvider)
        : base(options)
    {
        _auditaXOptions = auditaXOptions;
        _databaseProvider = databaseProvider;
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var tableName = _auditaXOptions.TableName;
        var schema = _auditaXOptions.Schema;
        var useJson = _auditaXOptions.ChangeLogFormat == ChangeLogFormat.Json;

        // Get column names from the database provider
        var logIdColumn = _databaseProvider.LogIdColumn;
        var sourceNameColumn = _databaseProvider.SourceNameColumn;
        var sourceKeyColumn = _databaseProvider.SourceKeyColumn;
        var auditLogColumn = _databaseProvider.AuditLogColumn;

        // Determine if PostgreSQL by checking column naming convention
        var isPostgreSql = logIdColumn == "log_id";
        var defaultValueSql = isPostgreSql ? "gen_random_uuid()" : "NEWID()";
        var columnType = isPostgreSql
            ? (useJson ? "JSONB" : "XML")
            : (useJson ? "NVARCHAR(MAX)" : "XML");

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable(tableName, schema);
            entity.HasKey(e => e.LogId);

            entity.Property(e => e.LogId)
                .HasColumnName(logIdColumn)
                .HasDefaultValueSql(defaultValueSql);

            entity.Property(e => e.SourceName)
                .HasColumnName(sourceNameColumn)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.SourceKey)
                .HasColumnName(sourceKeyColumn)
                .HasMaxLength(900)
                .IsRequired();

            entity.Property(e => e.AuditLogXml)
                .HasColumnName(auditLogColumn)
                .HasColumnType(columnType)
                .IsRequired();

            entity.HasIndex(e => new { e.SourceName, e.SourceKey })
                .IsUnique()
                .HasDatabaseName($"UQ_{tableName}_Source");
        });
    }
}
