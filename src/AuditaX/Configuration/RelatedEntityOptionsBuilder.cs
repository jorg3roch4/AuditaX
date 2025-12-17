using System.Linq.Expressions;

namespace AuditaX.Configuration;

/// <summary>
/// Builder for configuring a related entity using FluentAPI.
/// </summary>
/// <typeparam name="TParent">The parent entity type.</typeparam>
/// <typeparam name="TRelated">The related entity type.</typeparam>
public sealed class RelatedEntityOptionsBuilder<TParent, TRelated>
    where TParent : class
    where TRelated : class
{
    private readonly RelatedEntityOptions _options;
    private readonly EntityOptionsBuilder<TParent> _parentBuilder;
    private readonly AuditaXOptions _auditaXOptions;

    /// <summary>
    /// Initializes a new instance of the RelatedEntityOptionsBuilder.
    /// </summary>
    internal RelatedEntityOptionsBuilder(
        RelatedEntityOptions options,
        EntityOptionsBuilder<TParent> parentBuilder,
        AuditaXOptions auditaXOptions)
    {
        _options = options;
        _parentBuilder = parentBuilder;
        _auditaXOptions = auditaXOptions;
    }

    /// <summary>
    /// Specifies the property that references the parent entity.
    /// </summary>
    /// <typeparam name="TKey">The type of the key property.</typeparam>
    /// <param name="keySelector">Expression to select the parent key property.</param>
    /// <returns>This builder for chaining.</returns>
    public RelatedEntityOptionsBuilder<TParent, TRelated> WithParentKey<TKey>(
        Expression<Func<TRelated, TKey>> keySelector)
    {
        var compiled = keySelector.Compile();
        _options.ParentKeySelector = entity => compiled((TRelated)entity)?.ToString() ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Specifies the properties to audit for this related entity.
    /// These properties are captured for Added, Removed, and Updated actions.
    /// </summary>
    /// <param name="properties">The property names to audit.</param>
    /// <returns>This builder for chaining.</returns>
    public RelatedEntityOptionsBuilder<TParent, TRelated> Properties(params ReadOnlySpan<string> properties)
    {
        foreach (var property in properties)
        {
            _options.Properties.Add(property);
        }
        return this;
    }

    /// <summary>
    /// Configures a lookup to resolve values from another entity.
    /// Used to capture display values (e.g., RoleName) instead of foreign key IDs.
    /// </summary>
    /// <typeparam name="TLookup">The type of the lookup entity.</typeparam>
    /// <param name="lookupName">The name to identify this lookup (e.g., "Roles").</param>
    /// <returns>A builder for configuring the lookup.</returns>
    public LookupOptionsBuilder<TParent, TRelated, TLookup> WithLookup<TLookup>(string lookupName)
        where TLookup : class
    {
        var lookupOptions = new LookupOptions
        {
            EntityType = typeof(TLookup),
            EntityName = lookupName
        };

        _options.Lookups[lookupName] = lookupOptions;

        return new LookupOptionsBuilder<TParent, TRelated, TLookup>(lookupOptions, this);
    }

    /// <summary>
    /// Configures another related entity for the same parent.
    /// </summary>
    /// <typeparam name="TOtherRelated">The type of the other related entity.</typeparam>
    /// <param name="relatedName">The name to use for the related entity in audit logs.</param>
    /// <returns>A builder for configuring the other related entity.</returns>
    public RelatedEntityOptionsBuilder<TParent, TOtherRelated> WithRelatedEntity<TOtherRelated>(
        string relatedName)
        where TOtherRelated : class
    {
        return _parentBuilder.WithRelatedEntity<TOtherRelated>(relatedName);
    }

    /// <summary>
    /// Configures another entity for auditing.
    /// </summary>
    /// <typeparam name="TOther">The type of the other entity.</typeparam>
    /// <param name="displayName">Optional display name for the entity in audit logs.</param>
    /// <returns>A builder for configuring the other entity.</returns>
    public EntityOptionsBuilder<TOther> ConfigureEntity<TOther>(string? displayName = null)
        where TOther : class
    {
        return _auditaXOptions.ConfigureEntity<TOther>(displayName);
    }
}
