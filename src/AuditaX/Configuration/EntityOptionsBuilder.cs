using System.Linq.Expressions;
using AuditaX.Models;

namespace AuditaX.Configuration;

/// <summary>
/// Builder for configuring an auditable entity using FluentAPI.
/// </summary>
/// <typeparam name="TEntity">The entity type being configured.</typeparam>
public sealed class EntityOptionsBuilder<TEntity>
    where TEntity : class
{
    private readonly EntityOptions _options;
    private readonly AuditaXOptions _auditaXOptions;

    /// <summary>
    /// Initializes a new instance of the EntityOptionsBuilder.
    /// </summary>
    internal EntityOptionsBuilder(EntityOptions options, AuditaXOptions auditaXOptions)
    {
        _options = options;
        _auditaXOptions = auditaXOptions;
    }

    /// <summary>
    /// Specifies the property to use as the entity key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key property.</typeparam>
    /// <param name="keySelector">Expression to select the key property.</param>
    /// <returns>This builder for chaining.</returns>
    public EntityOptionsBuilder<TEntity> WithKey<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        var compiled = keySelector.Compile();
        _options.KeySelector = entity => compiled((TEntity)entity)?.ToString() ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Specifies the properties to audit for this entity.
    /// </summary>
    /// <param name="properties">The property names to audit.</param>
    /// <returns>This builder for chaining.</returns>
    public EntityOptionsBuilder<TEntity> Properties(params ReadOnlySpan<string> properties)
    {
        foreach (var property in properties)
        {
            _options.Properties.Add(property);
        }
        return this;
    }

    /// <summary>
    /// Configures a related entity for auditing.
    /// </summary>
    /// <typeparam name="TRelated">The type of the related entity.</typeparam>
    /// <param name="relatedName">The name to use for the related entity in audit logs.</param>
    /// <returns>A builder for configuring the related entity.</returns>
    public RelatedEntityOptionsBuilder<TEntity, TRelated> WithRelatedEntity<TRelated>(string relatedName)
        where TRelated : class
    {
        var relatedOptions = new RelatedEntityOptions
        {
            EntityType = typeof(TRelated),
            RelatedName = relatedName,
            ParentEntityOptions = _options
        };

        _options.RelatedEntities[relatedName] = relatedOptions;
        _auditaXOptions.RegisterRelatedEntity(typeof(TRelated), relatedOptions);

        return new RelatedEntityOptionsBuilder<TEntity, TRelated>(relatedOptions, this, _auditaXOptions);
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
