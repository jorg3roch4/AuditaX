using AuditaX.Models;

namespace AuditaX.Configuration;

/// <summary>
/// Configuration for a related entity that should be tracked in parent entity's audit log.
/// </summary>
public sealed class RelatedEntityConfiguration
{
    /// <summary>
    /// Gets or sets the name used for this related entity in audit logs.
    /// </summary>
    public required string RelatedName { get; set; }

    /// <summary>
    /// Gets or sets the related entity type.
    /// </summary>
    public required Type RelatedType { get; set; }

    /// <summary>
    /// Gets or sets the function to extract the parent entity key from the related entity.
    /// </summary>
    public Func<object, string> ParentKeySelector { get; set; } = _ => string.Empty;

    /// <summary>
    /// Gets or sets the function to map added entity fields.
    /// </summary>
    public Func<object, List<FieldChange>> OnAddedMapper { get; set; } = _ => [];

    /// <summary>
    /// Gets or sets the function to map removed entity fields.
    /// </summary>
    public Func<object, List<FieldChange>> OnRemovedMapper { get; set; } = _ => [];

    /// <summary>
    /// Gets or sets the parent entity configuration.
    /// </summary>
    public required AuditEntityConfiguration ParentConfiguration { get; set; }
}
