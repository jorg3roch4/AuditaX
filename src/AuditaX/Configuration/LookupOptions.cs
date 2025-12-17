using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AuditaX.Configuration;

/// <summary>
/// Configuration options for a lookup that resolves values from another entity.
/// Used to capture display values (e.g., RoleName) instead of foreign key IDs.
/// </summary>
public sealed class LookupOptions
{
    /// <summary>
    /// The foreign key property name in the related entity.
    /// This is the property that contains the ID to look up.
    /// Example: "RoleId" in UserRoles table.
    /// </summary>
    public string ForeignKey { get; set; } = string.Empty;

    /// <summary>
    /// The primary key property name in the lookup entity.
    /// This is the property used to match the foreign key value.
    /// Example: "RoleId" in Roles table.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The properties to capture from the lookup entity.
    /// These values will be included in the audit log.
    /// Example: ["RoleName"] from Roles table.
    /// </summary>
    public List<string> Properties { get; set; } = [];

    // ══════════════════════════════════════════════════════════
    // Runtime properties (not serialized)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// The lookup entity type. Set at runtime via Fluent API.
    /// </summary>
    [JsonIgnore]
    public Type? EntityType { get; set; }

    /// <summary>
    /// The name used to identify this lookup entity.
    /// Set from the dictionary key in appsettings.json or via Fluent API.
    /// </summary>
    [JsonIgnore]
    public string? EntityName { get; set; }

    /// <summary>
    /// Function to extract the foreign key value from the related entity.
    /// Set at runtime via Fluent API.
    /// </summary>
    [JsonIgnore]
    public Func<object, object?>? ForeignKeySelector { get; set; }

    /// <summary>
    /// Function to extract the primary key value from the lookup entity.
    /// Set at runtime via Fluent API.
    /// </summary>
    [JsonIgnore]
    public Func<object, object?>? KeySelector { get; set; }

    /// <summary>
    /// Indicates whether this lookup has been fully resolved (type and selectors).
    /// </summary>
    [JsonIgnore]
    public bool IsResolved { get; internal set; }

    // ══════════════════════════════════════════════════════════
    // Public methods
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Resolves all runtime properties for this lookup.
    /// Call this method when the lookup entity type has been determined.
    /// </summary>
    /// <param name="lookupEntityType">The resolved type of the lookup entity.</param>
    /// <param name="relatedEntityType">The type of the related entity containing the foreign key.</param>
    public void Resolve(Type lookupEntityType, Type relatedEntityType)
    {
        if (IsResolved)
            return;

        EntityType = lookupEntityType;
        ResolveForeignKeySelector(relatedEntityType);
        ResolveKeySelector();
        IsResolved = true;
    }

    /// <summary>
    /// Resolves the ForeignKeySelector using reflection when loaded from appsettings.
    /// </summary>
    /// <param name="relatedEntityType">The type of the related entity containing the foreign key.</param>
    public void ResolveForeignKeySelector(Type relatedEntityType)
    {
        if (ForeignKeySelector is not null || string.IsNullOrEmpty(ForeignKey))
            return;

        var property = relatedEntityType.GetProperty(ForeignKey);
        if (property is not null)
        {
            ForeignKeySelector = entity => property.GetValue(entity);
        }
    }

    /// <summary>
    /// Resolves the KeySelector using reflection when loaded from appsettings.
    /// </summary>
    public void ResolveKeySelector()
    {
        if (KeySelector is not null || EntityType is null || string.IsNullOrEmpty(Key))
            return;

        var property = EntityType.GetProperty(Key);
        if (property is not null)
        {
            KeySelector = entity => property.GetValue(entity);
        }
    }

    /// <summary>
    /// Gets the foreign key value from a related entity instance.
    /// </summary>
    public object? GetForeignKeyValue(object relatedEntity)
    {
        return ForeignKeySelector?.Invoke(relatedEntity);
    }

    /// <summary>
    /// Gets the primary key value from a lookup entity instance.
    /// </summary>
    public object? GetKeyValue(object lookupEntity)
    {
        return KeySelector?.Invoke(lookupEntity);
    }

    /// <summary>
    /// Gets the property values from a lookup entity instance.
    /// Returns a dictionary of property name to value.
    /// </summary>
    public Dictionary<string, string?> GetPropertyValues(object lookupEntity)
    {
        var values = new Dictionary<string, string?>();

        if (EntityType is null || Properties.Count == 0)
            return values;

        foreach (var propertyName in Properties)
        {
            var property = EntityType.GetProperty(propertyName);
            if (property is not null)
            {
                var value = property.GetValue(lookupEntity)?.ToString();
                values[propertyName] = value;
            }
        }

        return values;
    }
}
