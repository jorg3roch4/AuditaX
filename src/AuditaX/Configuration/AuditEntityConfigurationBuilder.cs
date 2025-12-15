using System.Linq.Expressions;
using AuditaX.Models;

namespace AuditaX.Configuration;

/// <summary>
/// Builder for configuring an auditable entity.
/// </summary>
/// <typeparam name="TEntity">The entity type being configured.</typeparam>
public sealed class AuditEntityConfigurationBuilder<TEntity>
    where TEntity : class
{
    private readonly AuditaXOptionsBuilder _optionsBuilder;
    private readonly AuditEntityConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the AuditEntityConfigurationBuilder.
    /// </summary>
    /// <param name="optionsBuilder">The parent options builder.</param>
    /// <param name="configuration">The entity configuration.</param>
    internal AuditEntityConfigurationBuilder(
        AuditaXOptionsBuilder optionsBuilder,
        AuditEntityConfiguration configuration)
    {
        _optionsBuilder = optionsBuilder;
        _configuration = configuration;
    }

    /// <summary>
    /// Specifies the property to use as the entity key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key property.</typeparam>
    /// <param name="keySelector">Expression to select the key property.</param>
    /// <returns>This builder for chaining.</returns>
    public AuditEntityConfigurationBuilder<TEntity> WithKey<TKey>(
        Expression<Func<TEntity, TKey>> keySelector)
    {
        var compiled = keySelector.Compile();
        _configuration.KeySelector = entity => compiled((TEntity)entity)?.ToString() ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Specifies the properties to audit for this entity.
    /// </summary>
    /// <param name="properties">The property names to audit.</param>
    /// <returns>This builder for chaining.</returns>
    public AuditEntityConfigurationBuilder<TEntity> AuditProperties(params ReadOnlySpan<string> properties)
    {
        foreach (var property in properties)
        {
            _configuration.AuditableProperties.Add(property);
        }
        return this;
    }

    /// <summary>
    /// Configures a related entity for auditing.
    /// </summary>
    /// <typeparam name="TRelated">The type of the related entity.</typeparam>
    /// <param name="relatedName">The name to use for the related entity in audit logs.</param>
    /// <returns>A builder for configuring the related entity.</returns>
    public RelatedEntityConfigurationBuilder<TEntity, TRelated> WithRelatedEntity<TRelated>(
        string relatedName)
        where TRelated : class
    {
        var relatedConfig = new RelatedEntityConfiguration
        {
            RelatedName = relatedName,
            RelatedType = typeof(TRelated),
            ParentConfiguration = _configuration
        };

        _configuration.RelatedEntities.Add(relatedConfig);
        _optionsBuilder.Options.RelatedConfigurations[typeof(TRelated)] = relatedConfig;

        return new RelatedEntityConfigurationBuilder<TEntity, TRelated>(
            _optionsBuilder,
            this,
            relatedConfig);
    }

    /// <summary>
    /// Configures another entity for auditing.
    /// </summary>
    /// <typeparam name="TOther">The type of the other entity.</typeparam>
    /// <param name="entityName">The name to use for the entity in audit logs.</param>
    /// <returns>A builder for configuring the other entity.</returns>
    public AuditEntityConfigurationBuilder<TOther> AuditEntity<TOther>(string entityName)
        where TOther : class
    {
        return _optionsBuilder.AuditEntity<TOther>(entityName);
    }
}
