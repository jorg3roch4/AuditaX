using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using AuditaX.Enums;
using AuditaX.Interfaces;

namespace AuditaX.Configuration;

/// <summary>
/// Runtime options for AuditaX, combining settings from appsettings and fluent configuration.
/// </summary>
public sealed class AuditaXOptions
{
    /// <summary>
    /// Enables logging for audit operations. Default is false.
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// Minimum log level for audit operations. Default is Information.
    /// </summary>
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

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
    /// Format for the ChangeLog field storage.
    /// Default is XML for backward compatibility with existing data.
    /// </summary>
    public ChangeLogFormat ChangeLogFormat { get; set; } = ChangeLogFormat.Xml;

    /// <summary>
    /// Entity configurations registered via fluent API.
    /// </summary>
    internal Dictionary<Type, AuditEntityConfiguration> EntityConfigurations { get; } = [];

    /// <summary>
    /// Related entity configurations registered via fluent API.
    /// </summary>
    internal Dictionary<Type, RelatedEntityConfiguration> RelatedConfigurations { get; } = [];

    /// <summary>
    /// User provider factory for dependency injection.
    /// </summary>
    internal Func<IServiceProvider, IAuditUserProvider>? UserProviderFactory { get; set; }

    /// <summary>
    /// Configures entities to be audited using the fluent API.
    /// </summary>
    /// <param name="configure">Action to configure entities.</param>
    /// <returns>This options instance for chaining.</returns>
    public AuditaXOptions ConfigureEntities(Action<AuditaXOptionsBuilder> configure)
    {
        var builder = new AuditaXOptionsBuilder(this);
        configure(builder);
        return this;
    }

    /// <summary>
    /// Gets the entity configuration for the specified type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The entity configuration, or null if not found.</returns>
    public AuditEntityConfiguration? GetEntityConfiguration(Type entityType)
    {
        return EntityConfigurations.TryGetValue(entityType, out var config) ? config : null;
    }

    /// <summary>
    /// Gets the related entity configuration for the specified type.
    /// </summary>
    /// <param name="entityType">The related entity type.</param>
    /// <returns>The related entity configuration, or null if not found.</returns>
    public RelatedEntityConfiguration? GetRelatedConfiguration(Type entityType)
    {
        return RelatedConfigurations.TryGetValue(entityType, out var config) ? config : null;
    }

    /// <summary>
    /// Determines if the specified type is configured as an auditable entity.
    /// </summary>
    /// <param name="entityType">The entity type to check.</param>
    /// <returns>True if the entity is auditable, otherwise false.</returns>
    public bool IsAuditableEntity(Type entityType)
    {
        return EntityConfigurations.ContainsKey(entityType);
    }

    /// <summary>
    /// Determines if the specified type is configured as a related entity.
    /// </summary>
    /// <param name="entityType">The entity type to check.</param>
    /// <returns>True if the entity is a related entity, otherwise false.</returns>
    public bool IsRelatedEntity(Type entityType)
    {
        return RelatedConfigurations.ContainsKey(entityType);
    }
}
