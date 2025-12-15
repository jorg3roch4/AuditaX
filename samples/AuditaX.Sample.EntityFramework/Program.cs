using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using AuditaX.Enums;
using AuditaX.Extensions;
using AuditaX.EntityFramework.Extensions;
using AuditaX.SqlServer.Extensions;
using AuditaX.PostgreSql.Extensions;
using AuditaX.Interfaces;
using AuditaX.Sample.EntityFramework.Data;
using AuditaX.Samples.Common;
using AuditaX.Samples.Common.Demo;
using AuditaX.Samples.Common.Entities;
using AuditaX.Samples.Common.Providers;

// Display header
AnsiConsole.Write(
    new FigletText("AuditaX")
        .LeftJustified()
        .Color(Color.Blue));

AnsiConsole.MarkupLine("[bold blue]Sample: Entity Framework[/]");
AnsiConsole.MarkupLine("[dim]Interactive demo for all Entity Framework combinations[/]\n");

// Select configuration (supports command line args for non-interactive mode)
// Usage: dotnet run -- [sqlserver|postgresql] [json|xml] [fluent|appsettings]
var config = args.Length >= 2 ? ParseConfiguration(args) : SelectConfiguration();

AnsiConsole.WriteLine();
AnsiConsole.Write(new Spectre.Console.Rule($"[yellow]{config.GetDisplayName()}[/]").RuleStyle("yellow"));
AnsiConsole.WriteLine();

// Build the host with selected configuration
var builder = Host.CreateApplicationBuilder(args);

var connectionString = config.GetConnectionString("EF");
AnsiConsole.MarkupLine($"[dim]Database: {config.GetDatabaseName("EF")}[/]");
AnsiConsole.MarkupLine($"[dim]Format: {config.Format}[/]");
AnsiConsole.MarkupLine($"[dim]Config Mode: {config.ConfigMode}[/]\n");

// Register DbContext (simulating what a real app would do)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (config.Database == DatabaseType.SqlServer)
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

// Configure AuditaX
ConfigureAuditaX(builder.Services, config);

var host = builder.Build();

try
{
    await host.StartAsync();

    using var scope = host.Services.CreateScope();
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<AppDbContext>();
    var auditService = services.GetRequiredService<IAuditService>();
    var auditQueryService = services.GetRequiredService<IAuditQueryService>();
    var auditValidator = services.GetRequiredService<IAuditStartupValidator>();

    // Create data operations and tables (this will drop/recreate the database)
    var dataOps = new EFDataOperations(context);
    await dataOps.EnsureTablesCreatedAsync();

    // Recreate the AuditLog table after database was dropped
    await auditValidator.ValidateAsync();

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

static void ConfigureAuditaX(IServiceCollection services, SampleConfiguration config)
{
    var tableName = config.Database == DatabaseType.SqlServer ? "AuditLog" : "audit_log";
    var schema = config.Database == DatabaseType.SqlServer ? "dbo" : "public";

    services.AddAuditaX(options =>
    {
        options.TableName = tableName;
        options.Schema = schema;
        options.AutoCreateTable = true;
        options.EnableLogging = true;
        options.ChangeLogFormat = config.Format;

        if (config.ConfigMode == ConfigurationMode.FluentApi)
        {
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
        }
    })
    // Extensible API: UseEntityFramework + UseSqlServer/UsePostgreSql
    .UseEntityFramework<AppDbContext>()
    .UseDatabase(config.Database)
    .ValidateOnStartup();  // Creates audit table if AutoCreateTable = true

    // Register user provider
    services.AddSingleton<IAuditUserProvider>(new SampleUserProvider());
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
