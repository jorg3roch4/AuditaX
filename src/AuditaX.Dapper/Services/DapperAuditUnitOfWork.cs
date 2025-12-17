using System.Linq;
using AuditaX.Configuration;
using AuditaX.Dapper.Interfaces;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;

namespace AuditaX.Dapper.Services;

/// <summary>
/// Implementation of IAuditUnitOfWork that provides audit logging for Dapper repositories.
/// </summary>
public class DapperAuditUnitOfWork(
    IAuditService auditService,
    IChangeLogService changeLogService,
    AuditaXOptions options,
    IAuditUserProvider userProvider) : IAuditUnitOfWork
{
    /// <inheritdoc />
    public async Task LogCreateAsync<T>(T entity) where T : class
    {
        var (sourceName, sourceKey) = GetEntityInfo(entity);
        var user = userProvider.GetCurrentUser();

        await auditService.LogCreateAsync(sourceName, sourceKey, user);
    }

    /// <inheritdoc />
    public async Task LogUpdateAsync<T>(T original, T modified) where T : class
    {
        var (sourceName, sourceKey) = GetEntityInfo(modified);
        var changes = GetFieldChanges(original, modified);

        if (changes.Count > 0)
        {
            var user = userProvider.GetCurrentUser();
            await auditService.LogUpdateAsync(sourceName, sourceKey, changes, user);
        }
    }

    /// <inheritdoc />
    public async Task LogDeleteAsync<T>(T entity) where T : class
    {
        var (sourceName, sourceKey) = GetEntityInfo(entity);
        var user = userProvider.GetCurrentUser();

        await auditService.LogDeleteAsync(sourceName, sourceKey, user);
    }

    /// <inheritdoc />
    public async Task LogRelatedAddedAsync<TParent, TRelated>(TParent parent, TRelated related)
        where TParent : class
        where TRelated : class
    {
        var (sourceName, sourceKey) = GetEntityInfo(parent);
        var (relatedName, fields) = GetRelatedEntityInfo(related);
        var user = userProvider.GetCurrentUser();

        await auditService.LogRelatedAsync(sourceName, sourceKey, AuditAction.Added, relatedName, fields, user);
    }

    /// <inheritdoc />
    public async Task LogRelatedAddedAsync<TParent, TRelated>(TParent parent, TRelated related, params object[] lookups)
        where TParent : class
        where TRelated : class
    {
        var (sourceName, sourceKey) = GetEntityInfo(parent);
        var relatedType = typeof(TRelated);
        var relatedConfig = options.GetRelatedEntity(relatedType);
        var relatedName = relatedConfig?.RelatedName ?? relatedType.Name;

        var fields = GetFieldsFromLookups(relatedConfig, lookups);
        var user = userProvider.GetCurrentUser();

        await auditService.LogRelatedAsync(sourceName, sourceKey, AuditAction.Added, relatedName, fields, user);
    }

    /// <inheritdoc />
    public async Task LogRelatedUpdatedAsync<TParent, TRelated>(TParent parent, TRelated original, TRelated modified)
        where TParent : class
        where TRelated : class
    {
        var (sourceName, sourceKey) = GetEntityInfo(parent);
        var relatedType = typeof(TRelated);
        var relatedConfig = options.GetRelatedEntity(relatedType);
        var relatedName = relatedConfig?.RelatedName ?? relatedType.Name;

        var changes = GetRelatedFieldChanges(original, modified, relatedConfig);

        if (changes.Count > 0)
        {
            var user = userProvider.GetCurrentUser();
            await auditService.LogRelatedAsync(sourceName, sourceKey, AuditAction.Updated, relatedName, changes, user);
        }
    }

    /// <inheritdoc />
    public async Task LogRelatedUpdatedAsync<TParent, TRelated>(
        TParent parent,
        TRelated original,
        TRelated modified,
        object[] originalLookups,
        object[] modifiedLookups)
        where TParent : class
        where TRelated : class
    {
        var (sourceName, sourceKey) = GetEntityInfo(parent);
        var relatedType = typeof(TRelated);
        var relatedConfig = options.GetRelatedEntity(relatedType);
        var relatedName = relatedConfig?.RelatedName ?? relatedType.Name;

        var changes = GetFieldChangesFromLookups(relatedConfig, originalLookups, modifiedLookups);

        if (changes.Count > 0)
        {
            var user = userProvider.GetCurrentUser();
            await auditService.LogRelatedAsync(sourceName, sourceKey, AuditAction.Updated, relatedName, changes, user);
        }
    }

    /// <inheritdoc />
    public async Task LogRelatedRemovedAsync<TParent, TRelated>(TParent parent, TRelated related)
        where TParent : class
        where TRelated : class
    {
        var (sourceName, sourceKey) = GetEntityInfo(parent);
        var (relatedName, fields) = GetRelatedEntityInfo(related);
        var user = userProvider.GetCurrentUser();

        await auditService.LogRelatedAsync(sourceName, sourceKey, AuditAction.Removed, relatedName, fields, user);
    }

    /// <inheritdoc />
    public async Task LogRelatedRemovedAsync<TParent, TRelated>(TParent parent, TRelated related, params object[] lookups)
        where TParent : class
        where TRelated : class
    {
        var (sourceName, sourceKey) = GetEntityInfo(parent);
        var relatedType = typeof(TRelated);
        var relatedConfig = options.GetRelatedEntity(relatedType);
        var relatedName = relatedConfig?.RelatedName ?? relatedType.Name;

        var fields = GetFieldsFromLookups(relatedConfig, lookups);
        var user = userProvider.GetCurrentUser();

        await auditService.LogRelatedAsync(sourceName, sourceKey, AuditAction.Removed, relatedName, fields, user);
    }

    private (string SourceName, string SourceKey) GetEntityInfo<T>(T entity) where T : class
    {
        var entityType = typeof(T);
        var config = options.GetEntity(entityType)
            ?? throw new InvalidOperationException(
                $"Entity type '{entityType.Name}' is not configured for auditing. " +
                $"Configure it using AuditaX options in appsettings.json or fluent API.");

        var sourceKey = config.GetKey(entity);

        return (config.DisplayName ?? entityType.Name, sourceKey);
    }

    private (string RelatedName, List<FieldChange> Fields) GetRelatedEntityInfo<T>(T entity) where T : class
    {
        var relatedType = typeof(T);
        var relatedConfig = options.GetRelatedEntity(relatedType);

        var relatedName = relatedConfig?.RelatedName ?? relatedType.Name;
        var fields = relatedConfig?.GetFieldChanges(entity) ?? [];

        return (relatedName, fields);
    }

    private List<FieldChange> GetFieldChanges<T>(T original, T modified) where T : class
    {
        var changes = new List<FieldChange>();
        var entityType = typeof(T);
        var config = options.GetEntity(entityType);

        if (config is null)
        {
            return changes;
        }

        foreach (var propertyName in config.Properties)
        {
            var property = entityType.GetProperty(propertyName);
            if (property is null)
            {
                continue;
            }

            var originalValue = property.GetValue(original);
            var modifiedValue = property.GetValue(modified);

            if (changeLogService.HasChanged(originalValue, modifiedValue))
            {
                changes.Add(new FieldChange
                {
                    Name = propertyName,
                    Before = changeLogService.ConvertToString(originalValue),
                    After = changeLogService.ConvertToString(modifiedValue)
                });
            }
        }

        return changes;
    }

    private List<FieldChange> GetRelatedFieldChanges<T>(T original, T modified, RelatedEntityOptions? config) where T : class
    {
        var changes = new List<FieldChange>();

        if (config is null)
        {
            return changes;
        }

        var relatedType = typeof(T);

        foreach (var propertyName in config.Properties)
        {
            var property = relatedType.GetProperty(propertyName);
            if (property is null)
            {
                continue;
            }

            var originalValue = property.GetValue(original);
            var modifiedValue = property.GetValue(modified);

            if (changeLogService.HasChanged(originalValue, modifiedValue))
            {
                changes.Add(new FieldChange
                {
                    Name = propertyName,
                    Before = changeLogService.ConvertToString(originalValue),
                    After = changeLogService.ConvertToString(modifiedValue)
                });
            }
        }

        return changes;
    }

    /// <summary>
    /// Extracts field values from lookup entities for Added/Removed actions.
    /// </summary>
    private List<FieldChange> GetFieldsFromLookups(RelatedEntityOptions? config, object[] lookups)
    {
        var fields = new List<FieldChange>();

        if (config is null || !config.HasLookups || lookups.Length == 0)
        {
            return fields;
        }

        foreach (var lookup in lookups)
        {
            var lookupType = lookup.GetType();

            // Find matching lookup configuration by type
            var lookupConfig = config.Lookups.Values
                .FirstOrDefault(l => l.EntityType == lookupType);

            if (lookupConfig is null)
            {
                continue;
            }

            // Extract configured properties from the lookup entity
            var propertyValues = lookupConfig.GetPropertyValues(lookup);
            foreach (var kvp in propertyValues)
            {
                fields.Add(new FieldChange
                {
                    Name = kvp.Key,
                    Value = kvp.Value
                });
            }
        }

        return fields;
    }

    /// <summary>
    /// Extracts field changes from lookup entities for Updated action.
    /// </summary>
    private List<FieldChange> GetFieldChangesFromLookups(
        RelatedEntityOptions? config,
        object[] originalLookups,
        object[] modifiedLookups)
    {
        var changes = new List<FieldChange>();

        if (config is null || !config.HasLookups)
        {
            return changes;
        }

        // Build dictionaries of property values by lookup type
        var originalValues = GetLookupPropertyValues(config, originalLookups);
        var modifiedValues = GetLookupPropertyValues(config, modifiedLookups);

        // Compare and create field changes
        var allPropertyNames = originalValues.Keys.Union(modifiedValues.Keys);

        foreach (var propertyName in allPropertyNames)
        {
            originalValues.TryGetValue(propertyName, out var originalValue);
            modifiedValues.TryGetValue(propertyName, out var modifiedValue);

            if (changeLogService.HasChanged(originalValue, modifiedValue))
            {
                changes.Add(new FieldChange
                {
                    Name = propertyName,
                    Before = originalValue,
                    After = modifiedValue
                });
            }
        }

        return changes;
    }

    /// <summary>
    /// Extracts all property values from lookup entities into a dictionary.
    /// </summary>
    private Dictionary<string, string?> GetLookupPropertyValues(RelatedEntityOptions config, object[] lookups)
    {
        var values = new Dictionary<string, string?>();

        foreach (var lookup in lookups)
        {
            var lookupType = lookup.GetType();

            var lookupConfig = config.Lookups.Values
                .FirstOrDefault(l => l.EntityType == lookupType);

            if (lookupConfig is null)
            {
                continue;
            }

            var propertyValues = lookupConfig.GetPropertyValues(lookup);
            foreach (var kvp in propertyValues)
            {
                values[kvp.Key] = kvp.Value;
            }
        }

        return values;
    }
}
