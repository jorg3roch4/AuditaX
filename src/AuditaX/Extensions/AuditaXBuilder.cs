using System;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using AuditaX.Configuration;
using AuditaX.Interfaces;
using AuditaX.Services;

namespace AuditaX.Extensions;

/// <summary>
/// Builder for configuring AuditaX services with fluent API.
/// </summary>
public sealed class AuditaXBuilder
{
    private readonly IServiceCollection _services;
    private readonly AuditaXOptions _options;
    private bool _startupValidationEnabled;
    private Action<object, DbConnection>? _dbContextConfigurator;

    /// <summary>
    /// Initializes a new instance of the AuditaXBuilder.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The audit options.</param>
    internal AuditaXBuilder(IServiceCollection services, AuditaXOptions options)
    {
        _services = services;
        _options = options;
    }

    /// <summary>
    /// Gets the service collection for extension methods.
    /// </summary>
    public IServiceCollection Services => _services;

    /// <summary>
    /// Gets the options for extension methods.
    /// </summary>
    public AuditaXOptions Options => _options;

    /// <summary>
    /// Gets the DbContext configurator registered by the database provider.
    /// This is used by AuditaX.EntityFramework to configure the DbContextOptionsBuilder.
    /// </summary>
    public Action<object, DbConnection>? DbContextConfigurator => _dbContextConfigurator;

    /// <summary>
    /// Sets the DbContext configurator. Called by database providers (SqlServer, PostgreSql, etc.)
    /// to register how to configure Entity Framework for their specific database.
    /// </summary>
    /// <param name="configurator">
    /// Action that receives a DbContextOptionsBuilder (as object to avoid EF dependency) and DbConnection.
    /// </param>
    /// <returns>This builder for chaining.</returns>
    public AuditaXBuilder SetDbContextConfigurator(Action<object, DbConnection> configurator)
    {
        _dbContextConfigurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
        return this;
    }

    /// <summary>
    /// Gets whether startup validation is enabled.
    /// </summary>
    public bool IsStartupValidationEnabled => _startupValidationEnabled;

    /// <summary>
    /// Configures the user provider for audit tracking.
    /// </summary>
    /// <typeparam name="TProvider">The type of the user provider.</typeparam>
    /// <returns>This builder for chaining.</returns>
    public AuditaXBuilder UseUserProvider<TProvider>()
        where TProvider : class, IAuditUserProvider
    {
        var descriptor = new ServiceDescriptor(
            typeof(IAuditUserProvider),
            typeof(TProvider),
            ServiceLifetime.Scoped);

        _services.Add(descriptor);

        return this;
    }

    /// <summary>
    /// Configures the user provider with a factory.
    /// </summary>
    /// <param name="factory">Factory function to create the user provider.</param>
    /// <returns>This builder for chaining.</returns>
    public AuditaXBuilder UseUserProvider(Func<IServiceProvider, IAuditUserProvider> factory)
    {
        _services.AddScoped(factory);
        return this;
    }

    /// <summary>
    /// Enables startup validation which checks that the audit table exists and
    /// the column type matches the configured LogFormat.
    /// If validation fails, the application will terminate with a critical error.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    /// <remarks>
    /// This validation runs as a hosted service when the application starts.
    /// It checks:
    /// <list type="bullet">
    ///   <item>That the audit table exists (throws AuditTableNotFoundException if not)</item>
    ///   <item>That the AuditLog column type matches the configured format (throws AuditColumnFormatMismatchException if not)</item>
    /// </list>
    /// If AutoCreateTable is true, the table will be created if it doesn't exist.
    /// </remarks>
    public AuditaXBuilder ValidateOnStartup()
    {
        if (_startupValidationEnabled)
        {
            return this;
        }

        _services.AddHostedService<AuditaXStartupHostedService>();
        _startupValidationEnabled = true;

        return this;
    }
}
