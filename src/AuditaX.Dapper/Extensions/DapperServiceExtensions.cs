using System;
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using AuditaX.Dapper.Interfaces;
using AuditaX.Dapper.Repositories;
using AuditaX.Dapper.Services;
using AuditaX.Dapper.Validators;
using AuditaX.Extensions;
using AuditaX.Interfaces;

namespace AuditaX.Dapper.Extensions;

/// <summary>
/// Extension methods for configuring Dapper as the ORM provider.
/// </summary>
public static class DapperServiceExtensions
{
    /// <summary>
    /// Configures AuditaX to use Dapper with an existing DapperContext or IDbConnection provider.
    /// This is the recommended approach as it reuses the connection context already registered in the application.
    /// </summary>
    /// <typeparam name="TContext">The DapperContext type registered in the application. Must have a CreateConnection() method.</typeparam>
    /// <param name="builder">The AuditaX builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Example usage:
    /// <code>
    /// // Your app already has DapperContext registered
    /// services.AddScoped&lt;DapperContext&gt;(sp => new DapperContext(connectionString));
    ///
    /// // AuditaX reuses it
    /// services.AddAuditaX(configuration)
    ///     .UseDapper&lt;DapperContext&gt;()
    ///     .UseSqlServer()
    ///     .ValidateOnStartup();
    /// </code>
    ///
    /// The TContext type must have a public CreateConnection() method that returns IDbConnection.
    /// </remarks>
    public static AuditaXBuilder UseDapper<TContext>(this AuditaXBuilder builder)
        where TContext : class
    {
        return builder.UseDapper(sp =>
        {
            var context = sp.GetRequiredService<TContext>();
            var method = typeof(TContext).GetMethod("CreateConnection")
                ?? throw new InvalidOperationException(
                    $"Type {typeof(TContext).Name} must have a public CreateConnection() method that returns IDbConnection.");

            return method.Invoke(context, null) as IDbConnection
                ?? throw new InvalidOperationException(
                    $"CreateConnection() method on {typeof(TContext).Name} returned null.");
        });
    }

    /// <summary>
    /// Configures AuditaX to use Dapper with a custom connection factory.
    /// Use this when you need custom logic to create the database connection.
    /// </summary>
    /// <param name="builder">The AuditaX builder.</param>
    /// <param name="connectionFactory">Factory function to create the database connection.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Example usage:
    /// <code>
    /// services.AddAuditaX(configuration)
    ///     .UseDapper(sp => sp.GetRequiredService&lt;DapperContext&gt;().CreateConnection())
    ///     .UseSqlServer()
    ///     .ValidateOnStartup();
    /// </code>
    /// </remarks>
    public static AuditaXBuilder UseDapper(
        this AuditaXBuilder builder,
        Func<IServiceProvider, IDbConnection> connectionFactory)
    {
        if (connectionFactory is null)
        {
            throw new ArgumentNullException(nameof(connectionFactory));
        }

        // Register Dapper-based repository
        builder.Services.AddScoped<IAuditRepository>(sp =>
        {
            var connection = connectionFactory(sp);
            var databaseProvider = sp.GetRequiredService<IDatabaseProvider>();
            return new DapperAuditRepository(connection, databaseProvider);
        });

        // Register Dapper-based startup validator
        builder.Services.AddScoped<IAuditStartupValidator>(sp =>
        {
            var connection = connectionFactory(sp);
            var databaseProvider = sp.GetRequiredService<IDatabaseProvider>();
            return new DapperAuditStartupValidator(connection, databaseProvider, builder.Options);
        });

        // Register Dapper-based query service
        builder.Services.AddScoped<IAuditQueryService>(sp =>
        {
            var connection = connectionFactory(sp);
            var databaseProvider = sp.GetRequiredService<IDatabaseProvider>();
            return new DapperAuditQueryService(connection, databaseProvider);
        });

        // Register IAuditUnitOfWork for use in repositories
        builder.Services.AddScoped<IAuditUnitOfWork, DapperAuditUnitOfWork>();

        return builder;
    }
}
