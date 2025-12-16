namespace AuditaX.Configuration;

/// <summary>
/// Configuration for an auditable entity.
/// </summary>
public sealed class AuditEntityConfiguration
{
    /// <summary>
    /// Gets or sets the name used for this entity in audit logs.
    /// </summary>
    public required string EntityName { get; set; }

    /// <summary>
    /// Gets or sets the entity type.
    /// May be null initially for configurations loaded from appsettings,
    /// but will be set when first accessed via GetEntityConfiguration.
    /// </summary>
    public Type? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the property name used to extract the entity key.
    /// Used by appsettings-based configurations for reflection-based key extraction.
    /// </summary>
    public string? KeyPropertyName { get; set; }

    /// <summary>
    /// Gets or sets the function to extract the entity key.
    /// </summary>
    public Func<object, string> KeySelector { get; set; } = _ => string.Empty;

    /// <summary>
    /// Gets or sets the set of property names to audit.
    /// </summary>
    public HashSet<string> AuditableProperties { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of related entity configurations.
    /// </summary>
    public List<RelatedEntityConfiguration> RelatedEntities { get; set; } = [];
}
