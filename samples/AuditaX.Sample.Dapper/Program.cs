using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Spectre.Console;
using AuditaX.Enums;
using AuditaX.Extensions;
using AuditaX.Dapper.Extensions;
using AuditaX.SqlServer.Extensions;
using AuditaX.PostgreSql.Extensions;
using AuditaX.Interfaces;
using AuditaX.Sample.Dapper.Data;
using AuditaX.Samples.Common;
using AuditaX.Samples.Common.Demo;
using AuditaX.Samples.Common.Entities;
using AuditaX.Samples.Common.Providers;

// Display header
AnsiConsole.Write(
    new FigletText("AuditaX")
        .LeftJustified()
        .Color(Color.Blue));

AnsiConsole.MarkupLine("[bold blue]Sample: Dapper[/]");
AnsiConsole.MarkupLine("[dim]Interactive demo for all Dapper combinations[/]\n");

// Select configuration (supports command line args for non-interactive mode)
// Usage: dotnet run -- [sqlserver|postgresql] [json|xml] [fluent|appsettings]
var config = args.Length >= 2 ? ParseConfiguration(args) : SelectConfiguration();

AnsiConsole.WriteLine();
AnsiConsole.Write(new Spectre.Console.Rule($"[yellow]{config.GetDisplayName()}[/]").RuleStyle("yellow"));
AnsiConsole.WriteLine();

// Build the host with selected configuration
var builder = Host.CreateApplicationBuilder(args);

var connectionString = config.GetConnectionString("Dapper");
AnsiConsole.MarkupLine($"[dim]Database: {config.GetDatabaseName("Dapper")}[/]");
AnsiConsole.MarkupLine($"[dim]Format: {config.Format}[/]");
AnsiConsole.MarkupLine($"[dim]Config Mode: {config.ConfigMode}[/]\n");

// Register DapperContext (simulating what a real app would do)
builder.Services.AddScoped<SampleDapperContext>(sp =>
    new SampleDapperContext(connectionString, config.Database));

// Configure AuditaX
ConfigureAuditaX(builder.Services, builder.Configuration, config);

var host = builder.Build();

try
{
    await host.StartAsync();

    using var scope = host.Services.CreateScope();
    var services = scope.ServiceProvider;

    var dapperContext = services.GetRequiredService<SampleDapperContext>();
    var auditService = services.GetRequiredService<IAuditService>();
    var auditQueryService = services.GetRequiredService<IAuditQueryService>();

    // Create data operations (tables are created by DatabaseSetup tool)
    using var connection = dapperContext.CreateConnection();
    var dataOps = new DapperDataOperations(connection, config.Database);

    // Run the demo
    var demoRunner = new DemoRunner(auditService, auditQueryService);
    await demoRunner.RunAsync(dataOps);
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"\n[red]Error: {ex.Message}[/]");
    AnsiConsole.MarkupLine($"[dim]{ex.StackTrace}[/]");

    if (ex.InnerException != null)
    {
        AnsiConsole.MarkupLine($"\n[red]Inner: {ex.InnerException.Message}[/]");
    }

    return 1;
}

return 0;

// ============================================================================
// Helper Methods
// ============================================================================

static SampleConfiguration SelectConfiguration()
{
    var database = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[yellow]Select Database:[/]")
            .AddChoices("SQL Server", "PostgreSQL"));

    var format = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[yellow]Select Audit Log Format:[/]")
            .AddChoices("JSON", "XML"));

    var configMode = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[yellow]Select Configuration Mode:[/]")
            .AddChoices("FluentApi (code)", "AppSettings (appsettings.json)"));

    return new SampleConfiguration
    {
        Database = database == "SQL Server" ? DatabaseType.SqlServer : DatabaseType.PostgreSql,
        Format = format == "JSON" ? ChangeLogFormat.Json : ChangeLogFormat.Xml,
        ConfigMode = configMode.StartsWith("FluentApi") ? ConfigurationMode.FluentApi : ConfigurationMode.AppSettings
    };
}

static SampleConfiguration ParseConfiguration(string[] args)
{
    var db = args[0].ToLowerInvariant();
    var fmt = args[1].ToLowerInvariant();
    var mode = args.Length >= 3 ? args[2].ToLowerInvariant() : "fluent";

    return new SampleConfiguration
    {
        Database = db == "postgresql" || db == "pg" ? DatabaseType.PostgreSql : DatabaseType.SqlServer,
        Format = fmt == "xml" ? ChangeLogFormat.Xml : ChangeLogFormat.Json,
        ConfigMode = mode == "appsettings" ? ConfigurationMode.AppSettings : ConfigurationMode.FluentApi
    };
}

static void ConfigureAuditaX(IServiceCollection services, IConfiguration configuration, SampleConfiguration config)
{
    var tableName = config.Database == DatabaseType.SqlServer ? "AuditLog" : "audit_log";
    var schema = config.Database == DatabaseType.SqlServer ? "dbo" : "public";

    AuditaX.Extensions.AuditaXBuilder builder;

    if (config.ConfigMode == ConfigurationMode.AppSettings)
    {
        // Load from appsettings.json and override database-specific values
        builder = services.AddAuditaX(configuration, options =>
        {
            options.TableName = tableName;
            options.Schema = schema;
            options.ChangeLogFormat = config.Format;
        });
    }
    else
    {
        // Fluent API configuration
        builder = services.AddAuditaX(options =>
        {
            options.TableName = tableName;
            options.Schema = schema;
            options.AutoCreateTable = true;
            options.EnableLogging = true;
            options.ChangeLogFormat = config.Format;

            options.ConfigureEntities(entities =>
            {
                entities.AuditEntity<Product>("Product")
                    .WithKey(p => p.Id)
                    .AuditProperties("Name", "Description", "Price", "Stock", "IsActive")
                    .WithRelatedEntity<ProductTag>("ProductTag")
                        .WithParentKey(t => t.ProductId)
                        .OnAdded(t => new Dictionary<string, string?> { ["Tag"] = t.Tag })
                        .OnRemoved(t => new Dictionary<string, string?> { ["Tag"] = t.Tag });
            });
        });
    }

    // Extensible API: UseDapper + UseSqlServer/UsePostgreSql
    builder
        .UseDapper<SampleDapperContext>()
        .UseDatabase(config.Database)
        .ValidateOnStartup();  // Creates audit table if AutoCreateTable = true

    // Register user provider
    services.AddSingleton<IAuditUserProvider>(new SampleUserProvider());
}

// ============================================================================
// Sample DapperContext (simulates what would exist in a real application)
// ============================================================================

/// <summary>
/// Sample DapperContext that demonstrates the pattern for AuditaX integration.
/// In a real application, this would be your existing DapperContext.
/// </summary>
public class SampleDapperContext : IDisposable
{
    private readonly string _connectionString;
    private readonly DatabaseType _databaseType;

    public SampleDapperContext(string connectionString, DatabaseType databaseType)
    {
        _connectionString = connectionString;
        _databaseType = databaseType;
    }

    /// <summary>
    /// Creates and opens a new database connection.
    /// This method is required by AuditaX's UseDapper&lt;TContext&gt;() method.
    /// </summary>
    public IDbConnection CreateConnection()
    {
        IDbConnection connection = _databaseType switch
        {
            DatabaseType.SqlServer => new SqlConnection(_connectionString),
            DatabaseType.PostgreSql => new NpgsqlConnection(_connectionString),
            _ => throw new NotSupportedException($"Database type {_databaseType} is not supported.")
        };

        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        return connection;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

// ============================================================================
// Extension method for dynamic database selection
// ============================================================================
public static class AuditaXBuilderDatabaseExtensions
{
    public static AuditaX.Extensions.AuditaXBuilder UseDatabase(
        this AuditaX.Extensions.AuditaXBuilder builder,
        DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.SqlServer => builder.UseSqlServer(),
            DatabaseType.PostgreSql => builder.UsePostgreSql(),
            _ => throw new NotSupportedException($"Database type {databaseType} is not supported.")
        };
    }
}
