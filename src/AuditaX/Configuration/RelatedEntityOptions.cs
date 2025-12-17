using System.Linq;
using System.Text.Json.Serialization;
using AuditaX.Models;

namespace AuditaX.Configuration;

/// <summary>
/// Configuration options for a related entity that should be tracked in parent entity's audit log.
/// Compatible with both FluentAPI and appsettings.json.
/// </summary>
public sealed class RelatedEntityOptions
{
    /// <summary>
    /// Property name that references the parent entity key.
    /// </summary>
    public string ParentKey { get; set; } = string.Empty;

    /// <summary>
    /// Properties to audit for this related entity.
    /// These are captured for Added, Removed, and Updated actions.
    /// </summary>
    public List<string> Properties { get; set; } = [];

    /// <summary>
    /// Lookup configurations for resolving values from other entities.
    /// Key is the lookup entity name (e.g., "Roles"), value is the lookup configuration.
    /// Used to capture display values (e.g., RoleName) instead of foreign key IDs.
    /// </summary>
    public Dictionary<string, LookupOptions> Lookups { get; set; } = [];

    // ══════════════════════════════════════════════════════════
    // Runtime properties (not serialized)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// The related entity type. Set at runtime.
    /// </summary>
    [JsonIgnore]
    public Type? EntityType { get; set; }

    /// <summary>
    /// The name used for this related entity in audit logs.
    /// </summary>
    [JsonIgnore]
    public string? RelatedName { get; set; }

    /// <summary>
    /// Reference to the parent entity configuration.
    /// </summary>
    [JsonIgnore]
    public EntityOptions? ParentEntityOptions { get; set; }

    /// <summary>
    /// Function to extract the parent entity key from the related entity.
    /// </summary>
    [JsonIgnore]
    public Func<object, string>? ParentKeySelector { get; set; }

    // ══════════════════════════════════════════════════════════
    // Public methods
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Resolves the ParentKeySelector using reflection when loaded from appsettings.
    /// </summary>
    public void ResolveParentKeySelector()
    {
        if (ParentKeySelector is not null || EntityType is null)
            return;

        var keyProperty = EntityType.GetProperty(ParentKey);
        if (keyProperty is not null)
        {
            ParentKeySelector = entity => keyProperty.GetValue(entity)?.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Resolves the lookup selectors using reflection when loaded from appsettings.
    /// Must be called after EntityType is set and lookup EntityTypes are resolved.
    /// </summary>
    public void ResolveLookupSelectors()
    {
        if (EntityType is null)
            return;

        foreach (var (name, lookup) in Lookups)
        {
            lookup.EntityName = name;
            lookup.ResolveForeignKeySelector(EntityType);
            lookup.ResolveKeySelector();
        }
    }

    /// <summary>
    /// Gets the parent key value from a related entity instance.
    /// </summary>
    public string GetParentKey(object entity)
    {
        return ParentKeySelector?.Invoke(entity) ?? string.Empty;
    }

    /// <summary>
    /// Checks if this related entity has any lookups configured.
    /// </summary>
    public bool HasLookups => Lookups.Count > 0;

    /// <summary>
    /// Checks if any lookups need to be resolved (EntityType not set).
    /// </summary>
    public bool HasUnresolvedLookups => Lookups.Values.Any(l => !l.IsResolved);

    /// <summary>
    /// Gets the lookup names that need to be resolved.
    /// </summary>
    public IEnumerable<string> GetUnresolvedLookupNames()
    {
        return Lookups
            .Where(kvp => !kvp.Value.IsResolved)
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Gets a lookup by name.
    /// </summary>
    public LookupOptions? GetLookup(string name)
    {
        return Lookups.TryGetValue(name, out var lookup) ? lookup : null;
    }

    /// <summary>
    /// Checks if a property should be audited.
    /// </summary>
    public bool ShouldAuditProperty(string propertyName)
    {
        return Properties.Count == 0 || Properties.Contains(propertyName);
    }

    /// <summary>
    /// Gets the field changes for an entity using reflection.
    /// Used for Added and Removed actions.
    /// </summary>
    public List<FieldChange> GetFieldChanges(object entity)
    {
        if (EntityType is null || Properties.Count == 0)
            return [];

        List<FieldChange> changes = [];

        foreach (var propertyName in Properties)
        {
            var property = EntityType.GetProperty(propertyName);
            if (property is null)
                continue;

            var value = property.GetValue(entity)?.ToString();
            changes.Add(new FieldChange
            {
                Name = propertyName,
                Value = value
            });
        }

        return changes;
    }

    /// <summary>
    /// Gets the field changes between original and modified entity.
    /// Used for Updated action.
    /// </summary>
    public List<FieldChange> GetFieldChanges(object original, object modified)
    {
        if (EntityType is null || Properties.Count == 0)
            return [];

        List<FieldChange> changes = [];

        foreach (var propertyName in Properties)
        {
            var property = EntityType.GetProperty(propertyName);
            if (property is null)
                continue;

            var originalValue = property.GetValue(original)?.ToString();
            var modifiedValue = property.GetValue(modified)?.ToString();

            if (originalValue != modifiedValue)
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
}
