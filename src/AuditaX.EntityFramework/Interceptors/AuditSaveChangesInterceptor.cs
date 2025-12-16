using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;

namespace AuditaX.EntityFramework.Interceptors;

/// <summary>
/// Entity Framework Core interceptor that automatically captures entity changes for auditing.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly AuditaXOptions _options;
    private readonly IChangeLogService _changeLogService;
    private readonly IAuditUserProvider _userProvider;

    /// <summary>
    /// Initializes a new instance of the AuditSaveChangesInterceptor.
    /// </summary>
    /// <param name="options">The audit options.</param>
    /// <param name="changeLogService">The change log service.</param>
    /// <param name="userProvider">The user provider.</param>
    public AuditSaveChangesInterceptor(
        AuditaXOptions options,
        IChangeLogService changeLogService,
        IAuditUserProvider userProvider)
    {
        _options = options;
        _changeLogService = changeLogService;
        _userProvider = userProvider;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            ProcessChanges(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ProcessChanges(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ProcessChanges(DbContext context)
    {
        var user = _userProvider.GetCurrentUser();
        var auditLogs = context.Set<Entities.AuditLog>();
        var entries = context.ChangeTracker.Entries().ToList();

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType();

            // Check if this is a configured auditable entity
            var entityOptions = _options.GetEntity(entityType);
            if (entityOptions is not null)
            {
                ProcessEntityChanges(context, entry, entityOptions, user, auditLogs);
                continue;
            }

            // Check if this is a configured related entity
            var relatedOptions = _options.GetRelatedEntity(entityType);
            if (relatedOptions is not null)
            {
                ProcessRelatedEntityChanges(context, entry, relatedOptions, user, auditLogs);
            }
        }
    }

    private void ProcessEntityChanges(
        DbContext context,
        EntityEntry entry,
        EntityOptions entityOptions,
        string user,
        DbSet<Entities.AuditLog> auditLogs)
    {
        var sourceKey = entityOptions.GetKey(entry.Entity);
        var displayName = entityOptions.DisplayName ?? entry.Entity.GetType().Name;

        switch (entry.State)
        {
            case EntityState.Added:
                CreateAuditEntry(auditLogs, displayName, sourceKey, AuditAction.Created, user);
                break;

            case EntityState.Modified:
                var changes = GetFieldChanges(entry, entityOptions);
                if (changes.Count > 0)
                {
                    UpdateAuditEntry(context, auditLogs, displayName, sourceKey, changes, user);
                }
                break;

            case EntityState.Deleted:
                UpdateAuditEntryWithAction(context, auditLogs, displayName, sourceKey, AuditAction.Deleted, user);
                break;
        }
    }

    private void ProcessRelatedEntityChanges(
        DbContext context,
        EntityEntry entry,
        RelatedEntityOptions relatedOptions,
        string user,
        DbSet<Entities.AuditLog> auditLogs)
    {
        var parentKey = relatedOptions.GetParentKey(entry.Entity);
        var parentOptions = relatedOptions.ParentEntityOptions;

        if (parentOptions is null)
            return;

        var parentDisplayName = parentOptions.DisplayName ?? "Unknown";
        var relatedName = relatedOptions.RelatedName ?? "Related";

        switch (entry.State)
        {
            case EntityState.Added:
                var addedFields = relatedOptions.GetFieldChanges(entry.Entity);
                if (addedFields.Count > 0)
                {
                    UpdateAuditEntryWithRelated(
                        context,
                        auditLogs,
                        parentDisplayName,
                        parentKey,
                        AuditAction.Added,
                        relatedName,
                        addedFields,
                        user);
                }
                break;

            case EntityState.Modified:
                var modifiedFields = GetRelatedFieldChanges(entry, relatedOptions);
                if (modifiedFields.Count > 0)
                {
                    UpdateAuditEntryWithRelated(
                        context,
                        auditLogs,
                        parentDisplayName,
                        parentKey,
                        AuditAction.Updated,
                        relatedName,
                        modifiedFields,
                        user);
                }
                break;

            case EntityState.Deleted:
                var removedFields = relatedOptions.GetFieldChanges(entry.Entity);
                if (removedFields.Count > 0)
                {
                    UpdateAuditEntryWithRelated(
                        context,
                        auditLogs,
                        parentDisplayName,
                        parentKey,
                        AuditAction.Removed,
                        relatedName,
                        removedFields,
                        user);
                }
                break;
        }
    }

    private List<FieldChange> GetFieldChanges(EntityEntry entry, EntityOptions entityOptions)
    {
        List<FieldChange> changes = [];

        foreach (var property in entry.Properties)
        {
            if (!entityOptions.ShouldAuditProperty(property.Metadata.Name))
            {
                continue;
            }

            if (!property.IsModified)
            {
                continue;
            }

            var originalValue = _changeLogService.ConvertToString(property.OriginalValue);
            var currentValue = _changeLogService.ConvertToString(property.CurrentValue);

            if (_changeLogService.HasChanged(property.OriginalValue, property.CurrentValue))
            {
                changes.Add(new FieldChange
                {
                    Name = property.Metadata.Name,
                    Before = originalValue,
                    After = currentValue
                });
            }
        }

        return changes;
    }

    private List<FieldChange> GetRelatedFieldChanges(EntityEntry entry, RelatedEntityOptions relatedOptions)
    {
        List<FieldChange> changes = [];

        foreach (var property in entry.Properties)
        {
            if (!relatedOptions.ShouldAuditProperty(property.Metadata.Name))
            {
                continue;
            }

            if (!property.IsModified)
            {
                continue;
            }

            var originalValue = _changeLogService.ConvertToString(property.OriginalValue);
            var currentValue = _changeLogService.ConvertToString(property.CurrentValue);

            if (_changeLogService.HasChanged(property.OriginalValue, property.CurrentValue))
            {
                changes.Add(new FieldChange
                {
                    Name = property.Metadata.Name,
                    Before = originalValue,
                    After = currentValue
                });
            }
        }

        return changes;
    }

    private void CreateAuditEntry(
        DbSet<Entities.AuditLog> auditLogs,
        string sourceName,
        string sourceKey,
        AuditAction action,
        string user)
    {
        var auditLogXml = _changeLogService.CreateEntry(null, user);

        auditLogs.Add(new Entities.AuditLog
        {
            SourceName = sourceName,
            SourceKey = sourceKey,
            AuditLogXml = auditLogXml
        });
    }

    private void UpdateAuditEntry(
        DbContext context,
        DbSet<Entities.AuditLog> auditLogs,
        string sourceName,
        string sourceKey,
        List<FieldChange> changes,
        string user)
    {
        var existingLog = auditLogs.Local
            .FirstOrDefault(a => a.SourceName == sourceName && a.SourceKey == sourceKey);

        if (existingLog is null)
        {
            existingLog = auditLogs
                .FirstOrDefault(a => a.SourceName == sourceName && a.SourceKey == sourceKey);
        }

        if (existingLog is not null)
        {
            existingLog.AuditLogXml = _changeLogService.UpdateEntry(
                existingLog.AuditLogXml,
                changes,
                user);
        }
        else
        {
            var auditLogXml = _changeLogService.UpdateEntry(null, changes, user);
            auditLogs.Add(new Entities.AuditLog
            {
                SourceName = sourceName,
                SourceKey = sourceKey,
                AuditLogXml = auditLogXml
            });
        }
    }

    private void UpdateAuditEntryWithAction(
        DbContext context,
        DbSet<Entities.AuditLog> auditLogs,
        string sourceName,
        string sourceKey,
        AuditAction action,
        string user)
    {
        var existingLog = auditLogs.Local
            .FirstOrDefault(a => a.SourceName == sourceName && a.SourceKey == sourceKey);

        if (existingLog is null)
        {
            existingLog = auditLogs
                .FirstOrDefault(a => a.SourceName == sourceName && a.SourceKey == sourceKey);
        }

        if (existingLog is not null)
        {
            existingLog.AuditLogXml = _changeLogService.DeleteEntry(
                existingLog.AuditLogXml,
                user);
        }
    }

    private void UpdateAuditEntryWithRelated(
        DbContext context,
        DbSet<Entities.AuditLog> auditLogs,
        string sourceName,
        string sourceKey,
        AuditAction action,
        string relatedName,
        List<FieldChange> fields,
        string user)
    {
        var existingLog = auditLogs.Local
            .FirstOrDefault(a => a.SourceName == sourceName && a.SourceKey == sourceKey);

        if (existingLog is null)
        {
            existingLog = auditLogs
                .FirstOrDefault(a => a.SourceName == sourceName && a.SourceKey == sourceKey);
        }

        if (existingLog is not null)
        {
            existingLog.AuditLogXml = _changeLogService.RelatedEntry(
                existingLog.AuditLogXml,
                action,
                relatedName,
                fields,
                user);
        }
    }
}
