using System;
using System.Collections.Generic;
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
    /// Optional property name used as the entity display key (identifier) for audit logs.
    /// When configured, the value of this property is written to <c>AuditLog.SourceKey</c>
    /// instead of the FK key. When <c>null</c>, identifier resolution falls back to <see cref="Key"/>.
    /// JSON-bindable from appsettings.
    /// </summary>
    public string? Identifier { get; set; }

    /// <summary>
    /// Optional property name used as the human-readable reference for audit logs.
    /// When configured, the value of this property is written to <c>AuditLog.SourceReference</c>
    /// as descriptive context, independent of <see cref="Identifier"/> / <see cref="Key"/>.
    /// JSON-bindable from appsettings.
    /// </summary>
    public string? Reference { get; set; }

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
    /// Function to extract the entity display identifier. Set by FluentAPI
    /// or resolved via reflection from <see cref="Identifier"/>.
    /// When <c>null</c>, <see cref="GetIdentifier"/> falls back to <see cref="GetKey"/>.
    /// </summary>
    [JsonIgnore]
    public Func<object, string>? IdentifierSelector { get; set; }

    /// <summary>
    /// Function to extract the entity human-readable reference. Set by FluentAPI
    /// or resolved via reflection from <see cref="Reference"/>.
    /// When <c>null</c>, <see cref="GetReference"/> returns <c>null</c>.
    /// </summary>
    [JsonIgnore]
    public Func<object, string>? ReferenceSelector { get; set; }

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
    /// Resolves the IdentifierSelector using reflection when loaded from appsettings.
    /// No-op when <see cref="Identifier"/> is null/empty, when <see cref="IdentifierSelector"/>
    /// is already set (Fluent API takes precedence over JSON), or when <see cref="EntityType"/>
    /// is not set.
    /// </summary>
    public void ResolveIdentifierSelector()
    {
        if (IdentifierSelector is not null)
            return;

        if (string.IsNullOrWhiteSpace(Identifier))
            return;

        if (EntityType is null)
            return;

        var identifierProperty = EntityType.GetProperty(Identifier);
        if (identifierProperty is not null)
        {
            IdentifierSelector = entity => identifierProperty.GetValue(entity)?.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Resolves the ReferenceSelector using reflection when loaded from appsettings.
    /// No-op when <see cref="Reference"/> is null/empty, when <see cref="ReferenceSelector"/>
    /// is already set (Fluent API takes precedence over JSON), or when <see cref="EntityType"/>
    /// is not set.
    /// </summary>
    public void ResolveReferenceSelector()
    {
        if (ReferenceSelector is not null)
            return;

        if (string.IsNullOrWhiteSpace(Reference))
            return;

        if (EntityType is null)
            return;

        var referenceProperty = EntityType.GetProperty(Reference);
        if (referenceProperty is not null)
        {
            ReferenceSelector = entity => referenceProperty.GetValue(entity)?.ToString() ?? string.Empty;
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

    /// <summary>
    /// Gets the display identifier value from an entity instance.
    /// Falls back to <see cref="GetKey"/> when no <see cref="IdentifierSelector"/> is configured.
    /// </summary>
    public string GetIdentifier(object entity)
    {
        return IdentifierSelector?.Invoke(entity) ?? GetKey(entity);
    }

    /// <summary>
    /// Gets the human-readable reference value from an entity instance.
    /// Returns <c>null</c> when no <see cref="ReferenceSelector"/> is configured.
    /// Result is silently truncated to <see cref="MaxReferenceLength"/> characters
    /// so callers never propagate a SQL truncation failure for an oversized reference.
    /// </summary>
    public string? GetReference(object entity)
    {
        var value = ReferenceSelector?.Invoke(entity);
        if (value is null) return null;
        return value.Length > MaxReferenceLength ? value.Substring(0, MaxReferenceLength) : value;
    }

    /// <summary>
    /// Maximum length of <c>SourceReference</c> stored in the audit log table.
    /// Matches the <see cref="AuditaX.Entities.AuditLog.SourceReference"/> column length.
    /// </summary>
    public const int MaxReferenceLength = 512;
}
