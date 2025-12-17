using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AuditaX.Entities;
using AuditaX.Enums;

namespace AuditaX.EntityFramework.Customizers;

/// <summary>
/// Model customizer that adds AuditLog entity configuration to the user's DbContext.
/// This runs once when the model is built, not on every SaveChanges.
/// </summary>
internal sealed class AuditaXModelCustomizer : ModelCustomizer
{
    public AuditaXModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        // Call base customizer first
        base.Customize(modelBuilder, context);

        // Add AuditLog entity configuration if AuditaX is configured
        if (AuditaXModelCustomizerOptions.IsConfigured)
        {
            ConfigureAuditLogEntity(modelBuilder);
        }
    }

    private void ConfigureAuditLogEntity(ModelBuilder modelBuilder)
    {
        var options = AuditaXModelCustomizerOptions.Options!;
        var databaseProvider = AuditaXModelCustomizerOptions.DatabaseProvider!;

        var tableName = options.TableName;
        var schema = options.Schema;
        var useJson = options.LogFormat == LogFormat.Json;

        // Get column names from the database provider
        var logIdColumn = databaseProvider.LogIdColumn;
        var sourceNameColumn = databaseProvider.SourceNameColumn;
        var sourceKeyColumn = databaseProvider.SourceKeyColumn;
        var auditLogColumn = databaseProvider.AuditLogColumn;

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
