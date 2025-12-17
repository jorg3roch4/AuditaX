using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using AuditaX.EntityFramework.Contexts;
using AuditaX.EntityFramework.Customizers;
using AuditaX.EntityFramework.Interceptors;
using AuditaX.EntityFramework.Repositories;
using AuditaX.EntityFramework.Services;
using AuditaX.EntityFramework.Validators;
using AuditaX.Configuration;
using AuditaX.Extensions;
using AuditaX.Interfaces;

namespace AuditaX.EntityFramework.Extensions;

/// <summary>
/// Extension methods for configuring Entity Framework Core as the ORM provider.
/// </summary>
public static class EntityFrameworkServiceExtensions
{
    /// <summary>
    /// Configures AuditaX to use Entity Framework Core with an existing DbContext.
    /// This is the recommended approach as it reuses the DbContext already registered in the application.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type registered in the application.</typeparam>
    /// <param name="builder">The AuditaX builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Example usage:
    /// <code>
    /// // Your app already has DbContext registered
    /// services.AddDbContext&lt;AppDbContext&gt;(options => options.UseSqlServer(connectionString));
    ///
    /// // AuditaX reuses it
    /// services.AddAuditaX(configuration)
    ///     .UseEntityFramework&lt;AppDbContext&gt;()
    ///     .UseSqlServer()
    ///     .ValidateOnStartup();
    /// </code>
    /// </remarks>
    public static AuditaXBuilder UseEntityFramework<TContext>(this AuditaXBuilder builder)
        where TContext : DbContext
    {
        return builder.UseEntityFramework(sp => sp.GetRequiredService<TContext>());
    }

    /// <summary>
    /// Configures AuditaX to use Entity Framework Core with a custom DbContext factory.
    /// Use this when you need custom logic to resolve the DbContext.
    /// </summary>
    /// <param name="builder">The AuditaX builder.</param>
    /// <param name="contextFactory">Factory function to resolve the DbContext.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Example usage:
    /// <code>
    /// services.AddAuditaX(configuration)
    ///     .UseEntityFramework(sp => sp.GetRequiredService&lt;AppDbContext&gt;())
    ///     .UseSqlServer()
    ///     .ValidateOnStartup();
    /// </code>
    /// </remarks>
    public static AuditaXBuilder UseEntityFramework(
        this AuditaXBuilder builder,
        Func<IServiceProvider, DbContext> contextFactory)
    {
        if (contextFactory is null)
        {
            throw new ArgumentNullException(nameof(contextFactory));
        }

        // Store the context factory and options for later use
        var auditaXOptions = builder.Options;

        // Create a factory for AuditaXDbContext that shares the connection with the user's DbContext
        AuditaXDbContext CreateAuditaXContext(IServiceProvider sp)
        {
            var userContext = contextFactory(sp);
            var databaseProvider = sp.GetRequiredService<IDatabaseProvider>();
            var connection = userContext.Database.GetDbConnection();

            var optionsBuilder = new DbContextOptionsBuilder<AuditaXDbContext>();

            // Use the configurator registered by the database provider (SqlServer, PostgreSql, etc.)
            var configurator = builder.DbContextConfigurator
                ?? throw new InvalidOperationException(
                    "No DbContext configurator found. Make sure to call UseSqlServer(), UsePostgreSql(), " +
                    "or another database provider extension method after UseEntityFramework().");

            configurator(optionsBuilder, connection);

            return new AuditaXDbContext(optionsBuilder.Options, auditaXOptions, databaseProvider);
        }

        // Register EF-based repository using AuditaXDbContext
        builder.Services.AddScoped<IAuditRepository>(sp =>
        {
            var auditContext = CreateAuditaXContext(sp);
            return new EfAuditRepository(auditContext);
        });

        // Register EF-based startup validator using user's DbContext for connection
        builder.Services.AddScoped<IAuditStartupValidator>(sp =>
        {
            var context = contextFactory(sp);
            var provider = sp.GetRequiredService<IDatabaseProvider>();
            return new EfAuditStartupValidator(context, provider, auditaXOptions);
        });

        // Register EF-based query service using AuditaXDbContext
        builder.Services.AddScoped<IAuditQueryService>(sp =>
        {
            var auditContext = CreateAuditaXContext(sp);
            var provider = sp.GetRequiredService<IDatabaseProvider>();
            return new EfAuditQueryService(auditContext, provider);
        });

        // Register the interceptor
        builder.Services.AddScoped<AuditSaveChangesInterceptor>();

        return builder;
    }

    /// <summary>
    /// Configures AuditaX for automatic audit logging on this DbContext.
    /// <para>
    /// <b>IMPORTANT:</b> This method is REQUIRED for Entity Framework Core automatic change tracking.
    /// Without it, entity changes will not be audited.
    /// </para>
    /// </summary>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <param name="serviceProvider">The service provider (use the (sp, options) overload of AddDbContext).</param>
    /// <returns>The options builder for chaining.</returns>
    /// <remarks>
    /// <para>This method performs two critical operations:</para>
    /// <list type="number">
    /// <item><description>Configures the model customizer to add AuditLog entity to your DbContext model automatically.</description></item>
    /// <item><description>Adds the SaveChanges interceptor that captures all entity changes.</description></item>
    /// </list>
    /// <para>
    /// Example usage:
    /// <code>
    /// // Configure AuditaX FIRST
    /// services.AddAuditaX(options => { ... })
    ///     .UseEntityFramework&lt;AppDbContext&gt;()
    ///     .UseSqlServer();
    ///
    /// // Then register DbContext WITH AuditaX
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
    /// {
    ///     options.UseSqlServer(connectionString);
    ///     options.UseAuditaX(sp);  // Required for automatic auditing
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    public static DbContextOptionsBuilder UseAuditaX(
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider)
    {
        // Configure the model customizer options (runs once)
        if (!AuditaXModelCustomizerOptions.IsConfigured)
        {
            var options = serviceProvider.GetRequiredService<AuditaXOptions>();
            var databaseProvider = serviceProvider.GetRequiredService<IDatabaseProvider>();

            AuditaXModelCustomizerOptions.Options = options;
            AuditaXModelCustomizerOptions.DatabaseProvider = databaseProvider;
        }

        // Replace the default model customizer with ours (adds AuditLog to the model)
        optionsBuilder.ReplaceService<IModelCustomizer, AuditaXModelCustomizer>();

        // Add the interceptor for automatic change tracking
        var interceptor = serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>();
        optionsBuilder.AddInterceptors(interceptor);

        return optionsBuilder;
    }
}
