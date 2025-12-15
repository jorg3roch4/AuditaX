using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AuditaX.Extensions;
using AuditaX.Interfaces;
using AuditaX.PostgreSql.Providers;

namespace AuditaX.PostgreSql.Extensions;

/// <summary>
/// Extension methods for configuring PostgreSQL as the database provider.
/// </summary>
public static class PostgreSqlServiceExtensions
{
    /// <summary>
    /// Configures AuditaX to use PostgreSQL as the database provider.
    /// </summary>
    /// <param name="builder">The AuditaX builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// This method registers the PostgreSQL database provider which generates
    /// PostgreSQL-specific SQL statements for audit operations.
    ///
    /// Example usage:
    /// <code>
    /// services.AddAuditaX(configuration)
    ///     .UseDapper&lt;DapperContext&gt;()
    ///     .UsePostgreSql()
    ///     .ValidateOnStartup();
    /// </code>
    /// </remarks>
    public static AuditaXBuilder UsePostgreSql(this AuditaXBuilder builder)
    {
        // Register the PostgreSQL database provider
        builder.Services.AddSingleton<IDatabaseProvider>(sp =>
            new PostgreSqlDatabaseProvider(builder.Options));

        // Register the DbContext configurator for Entity Framework support
        builder.SetDbContextConfigurator((optionsBuilder, connection) =>
        {
            if (optionsBuilder is DbContextOptionsBuilder dbOptionsBuilder)
            {
                dbOptionsBuilder.UseNpgsql(connection);
            }
        });

        return builder;
    }
}
