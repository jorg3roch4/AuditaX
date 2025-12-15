using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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
        var settings = new AuditaXSettings();
        configuration.GetSection(AuditaXSettings.SectionName).Bind(settings);

        var options = MapSettingsToOptions(settings);

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
        var settings = new AuditaXSettings();
        configuration.GetSection(AuditaXSettings.SectionName).Bind(settings);

        var options = MapSettingsToOptions(settings);

        // Apply fluent configuration on top of appsettings
        configure(options);

        return AddAuditaXCore(services, options);
    }

    private static AuditaXOptions MapSettingsToOptions(AuditaXSettings settings)
    {
        var options = new AuditaXOptions
        {
            EnableLogging = settings.EnableLogging,
            TableName = settings.TableName,
            Schema = settings.Schema,
            AutoCreateTable = settings.AutoCreateTable
        };

        // Parse log level
        if (Enum.TryParse<LogLevel>(settings.MinimumLogLevel, true, out var logLevel))
        {
            options.MinimumLogLevel = logLevel;
        }

        // Parse ChangeLog format
        if (Enum.TryParse<ChangeLogFormat>(settings.ChangeLogFormat, true, out var changeLogFormat))
        {
            options.ChangeLogFormat = changeLogFormat;
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
        // ChangeLogService requires AuditaXOptions to determine write format (XML/JSON)
        services.AddScoped<IChangeLogService>(sp => new ChangeLogService(sp.GetRequiredService<AuditaXOptions>()));
        services.AddScoped<IAuditService, AuditService>();

        // Register default anonymous user provider (can be overridden by UseUserProvider)
        services.TryAddScoped<IAuditUserProvider, AnonymousUserProvider>();

        // Note: Logging is provided by the host application.
        // AuditaX will use ILogger<T> if available and EnableLogging is true.

        return new AuditaXBuilder(services, options);
    }
}
