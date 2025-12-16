using System.Text.Json.Serialization;
using AuditaX.Models;

namespace AuditaX.Configuration;

/// <summary>
/// Configuration options for an auditable entity.
/// Compatible with both FluentAPI and appsettings.json.
/// </summary>
public sealed class EntityOptions
{
    /// <summary>
    /// Display name used for this entity in audit logs.
    /// If not specified, the entity class name will be used.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Property name used as the entity key. Default is "Id".
    /// </summary>
    public string Key { get; set; } = "Id";

    /// <summary>
    /// List of property names to audit.
    /// </summary>
    public List<string> Properties { get; set; } = [];

    /// <summary>
    /// Related entity configurations.
    /// Key is the related entity name, value is its configuration.
    /// </summary>
    public Dictionary<string, RelatedEntityOptions> RelatedEntities { get; set; } = [];

    // ══════════════════════════════════════════════════════════
    // Runtime properties (not serialized)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// The entity type. Set at runtime.
    /// </summary>
    [JsonIgnore]
    public Type? EntityType { get; set; }

    /// <summary>
    /// Function to extract the entity key. Set by FluentAPI or resolved via reflection.
    /// </summary>
    [JsonIgnore]
    public Func<object, string>? KeySelector { get; set; }

    /// <summary>
    /// Cached HashSet of properties for fast lookup.
    /// </summary>
    [JsonIgnore]
    private HashSet<string>? _propertiesSet;

    // ══════════════════════════════════════════════════════════
    // Public methods
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Resolves the KeySelector using reflection when loaded from appsettings.
    /// </summary>
    public void ResolveKeySelector()
    {
        if (KeySelector is not null || EntityType is null)
            return;

        var keyProperty = EntityType.GetProperty(Key);
        if (keyProperty is not null)
        {
            KeySelector = entity => keyProperty.GetValue(entity)?.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Checks if a property should be audited.
    /// </summary>
    public bool ShouldAuditProperty(string propertyName)
    {
        _propertiesSet ??= [.. Properties];
        return _propertiesSet.Contains(propertyName);
    }

    /// <summary>
    /// Gets the key value from an entity instance.
    /// </summary>
    public string GetKey(object entity)
    {
        return KeySelector?.Invoke(entity) ?? string.Empty;
    }
}
