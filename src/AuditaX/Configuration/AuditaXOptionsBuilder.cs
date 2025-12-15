using System;

namespace AuditaX.Configuration;

/// <summary>
/// Builder for configuring auditable entities using fluent API.
/// </summary>
public sealed class AuditaXOptionsBuilder
{
    private readonly AuditaXOptions _options;

    /// <summary>
    /// Initializes a new instance of the AuditaXOptionsBuilder.
    /// </summary>
    /// <param name="options">The options to configure.</param>
    internal AuditaXOptionsBuilder(AuditaXOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Configures an entity type for auditing.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to audit.</typeparam>
    /// <param name="entityName">The name to use for the entity in audit logs.</param>
    /// <returns>A builder for configuring the entity.</returns>
    public AuditEntityConfigurationBuilder<TEntity> AuditEntity<TEntity>(string entityName)
        where TEntity : class
    {
        var configuration = new AuditEntityConfiguration
        {
            EntityName = entityName,
            EntityType = typeof(TEntity)
        };

        _options.EntityConfigurations[typeof(TEntity)] = configuration;

        return new AuditEntityConfigurationBuilder<TEntity>(this, configuration);
    }

    /// <summary>
    /// Gets the underlying options.
    /// </summary>
    internal AuditaXOptions Options => _options;
}
