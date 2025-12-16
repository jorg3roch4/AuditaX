using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AuditaX.Configuration;
using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Providers;
using AuditaX.Services;

namespace AuditaX.Extensions;

/// <summary>
/// Extension methods for configuring AuditaX services.
/// </summary>
public static class AuditaXServiceExtensions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    private const string SectionName = "AuditaX";

    /// <summary>
    /// Adds AuditaX services with fluent configuration only.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure options.</param>
    /// <returns>An AuditaXBuilder for further configuration.</returns>
    public static AuditaXBuilder AddAuditaX(
        this IServiceCollection services,
        Action<AuditaXOptions> configure)
    {
        var options = new AuditaXOptions();
        configure(options);

        return AddAuditaXCore(services, options);
    }

    /// <summary>
    /// Adds AuditaX services with configuration from appsettings.json.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <returns>An AuditaXBuilder for further configuration.</returns>
    public static AuditaXBuilder AddAuditaX(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = LoadFromConfiguration(configuration);
        return AddAuditaXCore(services, options);
    }

    /// <summary>
    /// Adds AuditaX services with configuration from appsettings.json
    /// and additional fluent configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="configure">Action to configure additional options.</param>
    /// <returns>An AuditaXBuilder for further configuration.</returns>
    public static AuditaXBuilder AddAuditaX(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AuditaXOptions> configure)
    {
        var options = LoadFromConfiguration(configuration);

        // Apply fluent configuration on top of appsettings
        configure(options);

        return AddAuditaXCore(services, options);
    }

    /// <summary>
    /// Loads AuditaXOptions from IConfiguration.
    /// </summary>
    private static AuditaXOptions LoadFromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var options = new AuditaXOptions();

        // Bind simple properties
        if (section["EnableLogging"] is { } enableLogging)
            options.EnableLogging = bool.TryParse(enableLogging, out var el) && el;

        if (section["TableName"] is { } tableName)
            options.TableName = tableName;

        if (section["Schema"] is { } schema)
            options.Schema = schema;

        if (section["AutoCreateTable"] is { } autoCreate)
            options.AutoCreateTable = bool.TryParse(autoCreate, out var ac) && ac;

        if (section["LogFormat"] is { } logFormat)
        {
            if (Enum.TryParse<LogFormat>(logFormat, true, out var lf))
                options.LogFormat = lf;
        }

        // Bind entity configurations
        var entitiesSection = section.GetSection("Entities");
        foreach (var entitySection in entitiesSection.GetChildren())
        {
            var entityName = entitySection.Key;
            var entityOptions = new EntityOptions
            {
                DisplayName = entitySection["DisplayName"] ?? entityName,
                Key = entitySection["Key"] ?? "Id"
            };

            // Bind properties list
            var propertiesSection = entitySection.GetSection("Properties");
            foreach (var prop in propertiesSection.GetChildren())
            {
                if (prop.Value is not null)
                    entityOptions.Properties.Add(prop.Value);
            }

            // Bind related entities
            var relatedSection = entitySection.GetSection("RelatedEntities");
            foreach (var relatedEntitySection in relatedSection.GetChildren())
            {
                var relatedName = relatedEntitySection.Key;
                var relatedOptions = new RelatedEntityOptions
                {
                    RelatedName = relatedName,
                    ParentKey = relatedEntitySection["ParentKey"] ?? string.Empty,
                    ParentEntityOptions = entityOptions
                };

                // Bind properties
                var relatedPropertiesSection = relatedEntitySection.GetSection("Properties");
                foreach (var prop in relatedPropertiesSection.GetChildren())
                {
                    if (prop.Value is not null)
                        relatedOptions.Properties.Add(prop.Value);
                }

                entityOptions.RelatedEntities[relatedName] = relatedOptions;
            }

            options.Entities[entityName] = entityOptions;
        }

        return options;
    }

    private static AuditaXBuilder AddAuditaXCore(
        IServiceCollection services,
        AuditaXOptions options)
    {
        // Register options as singleton
        services.AddSingleton(options);

        // Register core services
        services.AddScoped<IChangeLogService>(sp =>
            new ChangeLogService(sp.GetRequiredService<AuditaXOptions>()));
        services.AddScoped<IAuditService, AuditService>();

        // Register default anonymous user provider (can be overridden by UseUserProvider)
        services.TryAddScoped<IAuditUserProvider, AnonymousUserProvider>();

        return new AuditaXBuilder(services, options);
    }
}
