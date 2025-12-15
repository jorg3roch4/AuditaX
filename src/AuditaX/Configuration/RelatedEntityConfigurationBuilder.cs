using System.Linq.Expressions;
using AuditaX.Models;

namespace AuditaX.Configuration;

/// <summary>
/// Builder for configuring a related entity.
/// </summary>
/// <typeparam name="TParent">The parent entity type.</typeparam>
/// <typeparam name="TRelated">The related entity type.</typeparam>
public sealed class RelatedEntityConfigurationBuilder<TParent, TRelated>
    where TParent : class
    where TRelated : class
{
    private readonly AuditaXOptionsBuilder _optionsBuilder;
    private readonly AuditEntityConfigurationBuilder<TParent> _parentBuilder;
    private readonly RelatedEntityConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the RelatedEntityConfigurationBuilder.
    /// </summary>
    /// <param name="optionsBuilder">The root options builder.</param>
    /// <param name="parentBuilder">The parent entity builder.</param>
    /// <param name="configuration">The related entity configuration.</param>
    internal RelatedEntityConfigurationBuilder(
        AuditaXOptionsBuilder optionsBuilder,
        AuditEntityConfigurationBuilder<TParent> parentBuilder,
        RelatedEntityConfiguration configuration)
    {
        _optionsBuilder = optionsBuilder;
        _parentBuilder = parentBuilder;
        _configuration = configuration;
    }

    /// <summary>
    /// Specifies the property that references the parent entity.
    /// </summary>
    /// <typeparam name="TKey">The type of the key property.</typeparam>
    /// <param name="keySelector">Expression to select the parent key property.</param>
    /// <returns>This builder for chaining.</returns>
    public RelatedEntityConfigurationBuilder<TParent, TRelated> WithParentKey<TKey>(
        Expression<Func<TRelated, TKey>> keySelector)
    {
        var compiled = keySelector.Compile();
        _configuration.ParentKeySelector = entity => compiled((TRelated)entity)?.ToString() ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Configures the fields to capture when a related entity is added.
    /// </summary>
    /// <param name="mapper">Function to map the entity to field values.</param>
    /// <returns>This builder for chaining.</returns>
    public RelatedEntityConfigurationBuilder<TParent, TRelated> OnAdded(
        Func<TRelated, Dictionary<string, string?>> mapper)
    {
        _configuration.OnAddedMapper = entity =>
        {
            var dict = mapper((TRelated)entity);
            List<FieldChange> changes = [];
            foreach (var kvp in dict)
            {
                changes.Add(new FieldChange
                {
                    Name = kvp.Key,
                    Value = kvp.Value
                });
            }
            return changes;
        };
        return this;
    }

    /// <summary>
    /// Configures the fields to capture when a related entity is removed.
    /// </summary>
    /// <param name="mapper">Function to map the entity to field values.</param>
    /// <returns>This builder for chaining.</returns>
    public RelatedEntityConfigurationBuilder<TParent, TRelated> OnRemoved(
        Func<TRelated, Dictionary<string, string?>> mapper)
    {
        _configuration.OnRemovedMapper = entity =>
        {
            var dict = mapper((TRelated)entity);
            List<FieldChange> changes = [];
            foreach (var kvp in dict)
            {
                changes.Add(new FieldChange
                {
                    Name = kvp.Key,
                    Value = kvp.Value
                });
            }
            return changes;
        };
        return this;
    }

    /// <summary>
    /// Configures another related entity for the same parent.
    /// </summary>
    /// <typeparam name="TOtherRelated">The type of the other related entity.</typeparam>
    /// <param name="relatedName">The name to use for the related entity in audit logs.</param>
    /// <returns>A builder for configuring the other related entity.</returns>
    public RelatedEntityConfigurationBuilder<TParent, TOtherRelated> WithRelatedEntity<TOtherRelated>(
        string relatedName)
        where TOtherRelated : class
    {
        return _parentBuilder.WithRelatedEntity<TOtherRelated>(relatedName);
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
