using System.Text.Json.Serialization;
using AuditaX.Enums;

namespace AuditaX.Configuration;

/// <summary>
/// Configuration options for AuditaX.
/// Compatible with both FluentAPI and appsettings.json.
/// </summary>
public sealed class AuditaXOptions
{
    // ══════════════════════════════════════════════════════════
    // Global configuration (serializable for appsettings.json)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Enables logging for audit operations. Default is false.
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// Name of the audit log table. Default is "AuditLog".
    /// </summary>
    public string TableName { get; set; } = "AuditLog";

    /// <summary>
    /// Database schema for the audit table. Default is "dbo".
    /// </summary>
    public string Schema { get; set; } = "dbo";

    /// <summary>
    /// Whether to automatically create the audit table if it doesn't exist.
    /// Default is false.
    /// </summary>
    public bool AutoCreateTable { get; set; } = false;

    /// <summary>
    /// Format for storing audit log entries.
    /// Default is Xml for backward compatibility.
    /// </summary>
    public LogFormat LogFormat { get; set; } = LogFormat.Xml;

    // ══════════════════════════════════════════════════════════
    // Entity configurations (serializable for appsettings.json)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Entity configurations.
    /// Key is the entity class name, value is its configuration.
    /// </summary>
    public Dictionary<string, EntityOptions> Entities { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // ══════════════════════════════════════════════════════════
    // Internal caches (not serialized)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Cache for entity configurations by Type for fast lookup.
    /// </summary>
    [JsonIgnore]
    internal Dictionary<Type, EntityOptions> TypeCache { get; } = [];

    /// <summary>
    /// Cache for related entity configurations by Type.
    /// </summary>
    [JsonIgnore]
    internal Dictionary<Type, RelatedEntityOptions> RelatedEntityCache { get; } = [];

    // ══════════════════════════════════════════════════════════
    // FluentAPI methods
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Configures an entity for auditing using FluentAPI.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to configure.</typeparam>
    /// <param name="displayName">Optional display name for the entity in audit logs.</param>
    /// <returns>A builder for further configuration.</returns>
    public EntityOptionsBuilder<TEntity> ConfigureEntity<TEntity>(string? displayName = null)
        where TEntity : class
    {
        var entityName = typeof(TEntity).Name;
        var options = new EntityOptions
        {
            DisplayName = displayName ?? entityName,
            EntityType = typeof(TEntity)
        };

        Entities[entityName] = options;
        TypeCache[typeof(TEntity)] = options;

        return new EntityOptionsBuilder<TEntity>(options, this);
    }

    // ══════════════════════════════════════════════════════════
    // Lookup methods
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the entity configuration for the specified type.
    /// </summary>
    /// <param name="type">The entity type.</param>
    /// <returns>The entity configuration, or null if not found.</returns>
    public EntityOptions? GetEntity(Type type)
    {
        // First check the type cache (FluentAPI configurations)
        if (TypeCache.TryGetValue(type, out var cached))
            return cached;

        // Fall back to name-based lookup (appsettings configurations)
        if (Entities.TryGetValue(type.Name, out var options))
        {
            // Set runtime properties
            options.EntityType = type;
            options.ResolveKeySelector();

            // Resolve related entities
            foreach (var (relatedName, relatedOptions) in options.RelatedEntities)
            {
                relatedOptions.RelatedName = relatedName;
                relatedOptions.ParentEntityOptions = options;
            }

            // Cache for future lookups
            TypeCache[type] = options;
            return options;
        }

        return null;
    }

    /// <summary>
    /// Gets the entity configuration by entity name.
    /// </summary>
    /// <param name="name">The entity name.</param>
    /// <returns>The entity configuration, or null if not found.</returns>
    public EntityOptions? GetEntity(string name)
    {
        return Entities.TryGetValue(name, out var options) ? options : null;
    }

    /// <summary>
    /// Gets the related entity configuration for the specified type.
    /// </summary>
    /// <param name="type">The related entity type.</param>
    /// <returns>The related entity configuration, or null if not found.</returns>
    public RelatedEntityOptions? GetRelatedEntity(Type type)
    {
        // First check the cache (FluentAPI configurations)
        if (RelatedEntityCache.TryGetValue(type, out var cached))
            return cached;

        // Fall back to name-based lookup in all entities (appsettings configurations)
        var typeName = type.Name;
        foreach (var (_, entityOptions) in Entities)
        {
            if (entityOptions.RelatedEntities.TryGetValue(typeName, out var relatedOptions))
            {
                // Set runtime properties
                relatedOptions.EntityType = type;
                relatedOptions.RelatedName = typeName;
                relatedOptions.ParentEntityOptions = entityOptions;
                relatedOptions.ResolveParentKeySelector();

                // Initialize lookup EntityNames from dictionary keys
                foreach (var (lookupName, lookupOptions) in relatedOptions.Lookups)
                {
                    lookupOptions.EntityName = lookupName;
                }

                // Cache for future lookups
                RelatedEntityCache[type] = relatedOptions;
                return relatedOptions;
            }
        }

        return null;
    }

    // ══════════════════════════════════════════════════════════
    // Internal methods
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Registers a related entity configuration in the cache.
    /// Called by EntityOptionsBuilder when configuring related entities via FluentAPI.
    /// </summary>
    internal void RegisterRelatedEntity(Type relatedType, RelatedEntityOptions options)
    {
        RelatedEntityCache[relatedType] = options;
    }
}
