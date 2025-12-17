using System;
using System.Linq.Expressions;

namespace AuditaX.Configuration;

/// <summary>
/// Builder for configuring a lookup using FluentAPI.
/// </summary>
/// <typeparam name="TParent">The parent entity type.</typeparam>
/// <typeparam name="TRelated">The related entity type containing the foreign key.</typeparam>
/// <typeparam name="TLookup">The lookup entity type to resolve values from.</typeparam>
public sealed class LookupOptionsBuilder<TParent, TRelated, TLookup>
    where TParent : class
    where TRelated : class
    where TLookup : class
{
    private readonly LookupOptions _options;
    private readonly RelatedEntityOptionsBuilder<TParent, TRelated> _relatedBuilder;

    /// <summary>
    /// Initializes a new instance of the LookupOptionsBuilder.
    /// </summary>
    internal LookupOptionsBuilder(
        LookupOptions options,
        RelatedEntityOptionsBuilder<TParent, TRelated> relatedBuilder)
    {
        _options = options;
        _relatedBuilder = relatedBuilder;
    }

    /// <summary>
    /// Specifies the foreign key property in the related entity.
    /// This is the property that contains the ID to look up.
    /// </summary>
    /// <typeparam name="TKey">The type of the foreign key.</typeparam>
    /// <param name="foreignKeySelector">Expression to select the foreign key property.</param>
    /// <returns>This builder for chaining.</returns>
    public LookupOptionsBuilder<TParent, TRelated, TLookup> ForeignKey<TKey>(
        Expression<Func<TRelated, TKey>> foreignKeySelector)
    {
        var memberExpression = foreignKeySelector.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a member access expression.", nameof(foreignKeySelector));

        _options.ForeignKey = memberExpression.Member.Name;

        var compiled = foreignKeySelector.Compile();
        _options.ForeignKeySelector = entity => compiled((TRelated)entity);

        return this;
    }

    /// <summary>
    /// Specifies the primary key property in the lookup entity.
    /// This is the property used to match the foreign key value.
    /// </summary>
    /// <typeparam name="TKey">The type of the primary key.</typeparam>
    /// <param name="keySelector">Expression to select the primary key property.</param>
    /// <returns>This builder for chaining.</returns>
    public LookupOptionsBuilder<TParent, TRelated, TLookup> Key<TKey>(
        Expression<Func<TLookup, TKey>> keySelector)
    {
        var memberExpression = keySelector.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a member access expression.", nameof(keySelector));

        _options.Key = memberExpression.Member.Name;

        var compiled = keySelector.Compile();
        _options.KeySelector = entity => compiled((TLookup)entity);

        return this;
    }

    /// <summary>
    /// Specifies the properties to capture from the lookup entity.
    /// These values will be included in the audit log.
    /// </summary>
    /// <param name="properties">The property names to capture.</param>
    /// <returns>The parent related entity builder for chaining.</returns>
    public RelatedEntityOptionsBuilder<TParent, TRelated> Properties(params ReadOnlySpan<string> properties)
    {
        foreach (var property in properties)
        {
            _options.Properties.Add(property);
        }

        // Mark as resolved since FluentAPI provides type-safe selectors
        _options.IsResolved = true;

        return _relatedBuilder;
    }
}
