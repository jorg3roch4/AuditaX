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
    /// Stores pending audit entries for Added entities.
    /// These are processed after SaveChanges when the database-generated IDs are available.
    /// </summary>
    private readonly List<PendingAuditEntry> _pendingAddedEntries = [];

    /// <summary>
    /// Represents a pending audit entry for an Added entity.
    /// </summary>
    private sealed class PendingAuditEntry
    {
        public required object Entity { get; init; }
        public required EntityOptions EntityOptions { get; init; }
        public required string User { get; init; }
    }

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

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is not null && _pendingAddedEntries.Count > 0)
        {
            ProcessPendingAddedEntries(eventData.Context);
            eventData.Context.SaveChanges();
        }

        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null && _pendingAddedEntries.Count > 0)
        {
            ProcessPendingAddedEntries(eventData.Context);
            await eventData.Context.SaveChangesAsync(cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Processes pending Added entities after SaveChanges when IDs are available.
    /// </summary>
    private void ProcessPendingAddedEntries(DbContext context)
    {
        var auditLogs = context.Set<Entities.AuditLog>();

        foreach (var pending in _pendingAddedEntries)
        {
            var sourceKey = pending.EntityOptions.GetKey(pending.Entity);
            var displayName = pending.EntityOptions.DisplayName ?? pending.Entity.GetType().Name;

            CreateAuditEntry(auditLogs, displayName, sourceKey, AuditAction.Created, pending.User);
        }

        _pendingAddedEntries.Clear();
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
        switch (entry.State)
        {
            case EntityState.Added:
                // Defer audit log creation for Added entities until SavedChanges
                // when the database-generated ID is available
                _pendingAddedEntries.Add(new PendingAuditEntry
                {
                    Entity = entry.Entity,
                    EntityOptions = entityOptions,
                    User = user
                });
                break;

            case EntityState.Modified:
                var sourceKey = entityOptions.GetKey(entry.Entity);
                var displayName = entityOptions.DisplayName ?? entry.Entity.GetType().Name;
                var changes = GetFieldChanges(entry, entityOptions);
                if (changes.Count > 0)
                {
                    UpdateAuditEntry(context, auditLogs, displayName, sourceKey, changes, user);
                }
                break;

            case EntityState.Deleted:
                var deleteSourceKey = entityOptions.GetKey(entry.Entity);
                var deleteDisplayName = entityOptions.DisplayName ?? entry.Entity.GetType().Name;
                UpdateAuditEntryWithAction(context, auditLogs, deleteDisplayName, deleteSourceKey, AuditAction.Deleted, user);
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
                var addedFields = GetFieldsWithLookups(context, entry.Entity, relatedOptions);
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
                var removedFields = GetFieldsWithLookups(context, entry.Entity, relatedOptions);
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

    private List<FieldChange> GetFieldsWithLookups(
        DbContext context,
        object relatedEntity,
        RelatedEntityOptions relatedOptions)
    {
        // Start with regular field changes
        var fields = relatedOptions.GetFieldChanges(relatedEntity);

        // Resolve lookup values if configured
        if (relatedOptions.HasLookups)
        {
            var lookupFields = ResolveLookupValues(context, relatedEntity, relatedOptions);
            fields.AddRange(lookupFields);
        }

        return fields;
    }

    private List<FieldChange> ResolveLookupValues(
        DbContext context,
        object relatedEntity,
        RelatedEntityOptions relatedOptions)
    {
        List<FieldChange> lookupFields = [];

        // Resolve any unresolved lookups using EF Core Model
        if (relatedOptions.HasUnresolvedLookups && relatedOptions.EntityType is not null)
        {
            ResolveLookupTypes(context, relatedOptions);
        }

        foreach (var (_, lookupOptions) in relatedOptions.Lookups)
        {
            if (lookupOptions.EntityType is null)
                continue;

            // Get the foreign key value from the related entity
            var foreignKeyValue = lookupOptions.GetForeignKeyValue(relatedEntity);
            if (foreignKeyValue is null)
                continue;

            // Find the lookup entity in the DbContext
            var lookupEntity = FindLookupEntity(context, lookupOptions, foreignKeyValue);
            if (lookupEntity is null)
                continue;

            // Get the property values from the lookup entity
            var propertyValues = lookupOptions.GetPropertyValues(lookupEntity);
            foreach (var (propertyName, value) in propertyValues)
            {
                lookupFields.Add(new FieldChange
                {
                    Name = propertyName,
                    Value = value
                });
            }
        }

        return lookupFields;
    }

    private void ResolveLookupTypes(DbContext context, RelatedEntityOptions relatedOptions)
    {
        if (relatedOptions.EntityType is null)
            return;

        foreach (var lookupName in relatedOptions.GetUnresolvedLookupNames())
        {
            var lookupOptions = relatedOptions.GetLookup(lookupName);
            if (lookupOptions is null)
                continue;

            // Try to find the entity type in EF Core Model by name
            var lookupEntityType = FindEntityTypeByName(context, lookupName);
            if (lookupEntityType is not null)
            {
                lookupOptions.Resolve(lookupEntityType, relatedOptions.EntityType);
            }
        }
    }

    private Type? FindEntityTypeByName(DbContext context, string entityName)
    {
        // Try to find by CLR type name (e.g., "Role", "ApplicationRole")
        var efEntityType = context.Model.GetEntityTypes()
            .FirstOrDefault(e =>
                e.ClrType.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));

        if (efEntityType is not null)
            return efEntityType.ClrType;

        // Try to find by table name (e.g., "Roles", "AspNetRoles")
        efEntityType = context.Model.GetEntityTypes()
            .FirstOrDefault(e =>
                e.GetTableName()?.Equals(entityName, StringComparison.OrdinalIgnoreCase) == true);

        return efEntityType?.ClrType;
    }

    private object? FindLookupEntity(
        DbContext context,
        LookupOptions lookupOptions,
        object foreignKeyValue)
    {
        if (lookupOptions.EntityType is null)
            return null;

        // First try to find in local cache (already tracked entities)
        var localEntities = context.ChangeTracker.Entries()
            .Where(e => e.Entity.GetType() == lookupOptions.EntityType)
            .Select(e => e.Entity);

        foreach (var entity in localEntities)
        {
            var keyValue = lookupOptions.GetKeyValue(entity);
            if (keyValue is not null && keyValue.Equals(foreignKeyValue))
            {
                return entity;
            }
        }

        // If not found locally, query the database
        try
        {
            return context.Find(lookupOptions.EntityType, foreignKeyValue);
        }
        catch
        {
            // If Find fails (e.g., composite key), return null
            return null;
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
        else
        {
            // Create new AuditLog for pre-existing records
            var auditLogXml = _changeLogService.DeleteEntry(null, user);
            auditLogs.Add(new Entities.AuditLog
            {
                SourceName = sourceName,
                SourceKey = sourceKey,
                AuditLogXml = auditLogXml
            });
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
        else
        {
            // Create new AuditLog for pre-existing records
            var auditLogXml = _changeLogService.RelatedEntry(
                null,
                action,
                relatedName,
                fields,
                user);
            auditLogs.Add(new Entities.AuditLog
            {
                SourceName = sourceName,
                SourceKey = sourceKey,
                AuditLogXml = auditLogXml
            });
        }
    }
}
