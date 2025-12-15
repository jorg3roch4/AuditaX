using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AuditaX.Extensions;
using AuditaX.Interfaces;
using AuditaX.SqlServer.Providers;

namespace AuditaX.SqlServer.Extensions;

/// <summary>
/// Extension methods for configuring SQL Server as the database provider.
/// </summary>
public static class SqlServerServiceExtensions
{
    /// <summary>
    /// Configures AuditaX to use SQL Server as the database provider.
    /// </summary>
    /// <param name="builder">The AuditaX builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// This method registers the SQL Server database provider which generates
    /// SQL Server-specific SQL statements for audit operations.
    ///
    /// Example usage:
    /// <code>
    /// services.AddAuditaX(configuration)
    ///     .UseDapper&lt;DapperContext&gt;()
    ///     .UseSqlServer()
    ///     .ValidateOnStartup();
    /// </code>
    /// </remarks>
    public static AuditaXBuilder UseSqlServer(this AuditaXBuilder builder)
    {
        // Register the SQL Server database provider
        builder.Services.AddSingleton<IDatabaseProvider>(sp =>
            new SqlServerDatabaseProvider(builder.Options));

        // Register the DbContext configurator for Entity Framework support
        builder.SetDbContextConfigurator((optionsBuilder, connection) =>
        {
            if (optionsBuilder is DbContextOptionsBuilder dbOptionsBuilder)
            {
                dbOptionsBuilder.UseSqlServer(connection);
            }
        });

        return builder;
    }
}
