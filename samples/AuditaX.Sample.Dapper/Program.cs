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
using AuditaX.Dapper.Interfaces;
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

var connectionString = config.GetConnectionString();
var tableName = config.GetAuditLogTableName("D"); // D = Dapper
var schema = config.GetSchema();

AnsiConsole.MarkupLine($"[dim]Database: {config.GetDatabaseName()}[/]");
AnsiConsole.MarkupLine($"[dim]AuditLog Table: {tableName}[/]");
AnsiConsole.MarkupLine($"[dim]Format: {config.Format}[/]");
AnsiConsole.MarkupLine($"[dim]Config Mode: {config.ConfigMode}[/]\n");

// Register DapperContext (simulating what a real app would do)
builder.Services.AddScoped<SampleDapperContext>(sp =>
    new SampleDapperContext(connectionString, config.Database));

// Configure AuditaX
ConfigureAuditaX(builder.Services, builder.Configuration, config, tableName, schema);

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

    // Additional Demo: IAuditUnitOfWork for Related Entities
    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Spectre.Console.Rule("[cyan]IAuditUnitOfWork Demo[/]").RuleStyle("cyan"));
    AnsiConsole.MarkupLine("\n[dim]The IAuditUnitOfWork interface provides a simpler API for auditing in Dapper repositories.[/]");
    AnsiConsole.MarkupLine("[dim]It automatically handles entity configuration and user context.[/]\n");

    var auditUnitOfWork = services.GetRequiredService<IAuditUnitOfWork>();
    await RunAuditUnitOfWorkDemo(dataOps, auditUnitOfWork, auditService);

    // Demo 8-12: User/Role/UserRole with Lookups (Identity-like scenario)
    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Spectre.Console.Rule("[bold magenta]Identity-like Demo: User Roles with Lookups[/]").RuleStyle("magenta"));
    AnsiConsole.MarkupLine("\n[dim]Demonstrates auditing User/Role/UserRole relationships with Lookup resolution.[/]");
    AnsiConsole.MarkupLine("[dim]Lookups allow showing 'RoleName: Administrator' instead of 'RoleId: guid...'[/]\n");

    await RunUserRoleDemo(dataOps, auditUnitOfWork, auditService);
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
        Format = format == "JSON" ? LogFormat.Json : LogFormat.Xml,
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
        Format = fmt == "xml" ? LogFormat.Xml : LogFormat.Json,
        ConfigMode = mode == "appsettings" ? ConfigurationMode.AppSettings : ConfigurationMode.FluentApi
    };
}

static void ConfigureAuditaX(IServiceCollection services, IConfiguration configuration, SampleConfiguration config, string tableName, string schema)
{
    AuditaX.Extensions.AuditaXBuilder builder;

    if (config.ConfigMode == ConfigurationMode.AppSettings)
    {
        // Load from appsettings.json and override database-specific values
        builder = services.AddAuditaX(configuration, options =>
        {
            options.TableName = tableName;
            options.Schema = schema;
            options.LogFormat = config.Format;
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
            options.LogFormat = config.Format;

            options.ConfigureEntity<Product>("Product")
                .WithKey(p => p.Id)
                .Properties("Name", "Description", "Price", "Stock", "IsActive")
                .WithRelatedEntity<ProductTag>("ProductTag")
                    .WithParentKey(t => t.ProductId)
                    .Properties("Tag");

            // User configuration with Related Entity and Lookup (Identity-like scenario)
            // With Lookups, the audit log shows "RoleName: Administrator" instead of "RoleId: guid..."
            options.ConfigureEntity<User>("User")
                .WithKey(u => u.UserId)
                .Properties("UserName", "Email", "PhoneNumber", "IsActive")
                .WithRelatedEntity<UserRole>("UserRoles")
                    .WithParentKey(ur => ur.UserId)
                    .WithLookup<Role>("Role")
                        .ForeignKey(ur => ur.RoleId)
                        .Key(r => r.RoleId)
                        .Properties("RoleName");
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
// IAuditUnitOfWork Demo for Related Entities
// ============================================================================
static async Task RunAuditUnitOfWorkDemo(
    IDemoDataOperations dataOps,
    IAuditUnitOfWork auditUnitOfWork,
    IAuditService auditService)
{
    // Create a new product for this demo
    AnsiConsole.MarkupLine("[bold cyan]--- Demo: Create Product with IAuditUnitOfWork ---[/]");
    var product = new Product
    {
        Name = "Mechanical Keyboard",
        Description = "RGB mechanical keyboard with Cherry MX switches",
        Price = 149.99m,
        Stock = 50,
        IsActive = true
    };

    product = await dataOps.CreateProductAsync(product);
    await auditUnitOfWork.LogCreateAsync(product);

    AnsiConsole.MarkupLine($"Created Product ID: [yellow]{product.Id}[/]");
    AnsiConsole.MarkupLine("[dim]Used: auditUnitOfWork.LogCreateAsync(product)[/]\n");

    // Demo: LogRelatedAddedAsync
    AnsiConsole.MarkupLine("[bold cyan]--- Demo: LogRelatedAddedAsync ---[/]");
    var tag1 = new ProductTag { ProductId = product.Id, Tag = "Keyboard" };
    tag1 = await dataOps.CreateProductTagAsync(tag1);
    await auditUnitOfWork.LogRelatedAddedAsync(product, tag1);

    AnsiConsole.MarkupLine($"Added Tag: [yellow]{tag1.Tag}[/]");
    AnsiConsole.MarkupLine("[dim]Used: auditUnitOfWork.LogRelatedAddedAsync(product, tag)[/]\n");

    var tag2 = new ProductTag { ProductId = product.Id, Tag = "Mechanical" };
    tag2 = await dataOps.CreateProductTagAsync(tag2);
    await auditUnitOfWork.LogRelatedAddedAsync(product, tag2);

    AnsiConsole.MarkupLine($"Added Tag: [yellow]{tag2.Tag}[/]");
    AnsiConsole.MarkupLine("[dim]Used: auditUnitOfWork.LogRelatedAddedAsync(product, tag)[/]\n");

    // Demo: LogRelatedUpdatedAsync (simulate tag update)
    AnsiConsole.MarkupLine("[bold cyan]--- Demo: LogRelatedUpdatedAsync ---[/]");
    var originalTag = new ProductTag { ProductId = product.Id, Tag = tag2.Tag };
    tag2.Tag = "Cherry MX";
    // In real code, you'd update the database here
    await auditUnitOfWork.LogRelatedUpdatedAsync(product, originalTag, tag2);

    AnsiConsole.MarkupLine($"Updated Tag: [yellow]Mechanical -> Cherry MX[/]");
    AnsiConsole.MarkupLine("[dim]Used: auditUnitOfWork.LogRelatedUpdatedAsync(product, originalTag, modifiedTag)[/]\n");

    // Demo: LogRelatedRemovedAsync
    AnsiConsole.MarkupLine("[bold cyan]--- Demo: LogRelatedRemovedAsync ---[/]");
    await dataOps.DeleteProductTagAsync(tag1.Id);
    await auditUnitOfWork.LogRelatedRemovedAsync(product, tag1);

    AnsiConsole.MarkupLine($"Removed Tag: [yellow]{tag1.Tag}[/]");
    AnsiConsole.MarkupLine("[dim]Used: auditUnitOfWork.LogRelatedRemovedAsync(product, tag)[/]\n");

    // Demo: LogUpdateAsync
    AnsiConsole.MarkupLine("[bold cyan]--- Demo: LogUpdateAsync ---[/]");
    var originalProduct = new Product
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        Stock = product.Stock,
        IsActive = product.IsActive
    };

    product.Price = 129.99m;
    product.Stock = 45;
    await dataOps.UpdateProductAsync(product);
    await auditUnitOfWork.LogUpdateAsync(originalProduct, product);

    AnsiConsole.MarkupLine($"Updated Product - Price: [yellow]{product.Price:C}[/], Stock: [yellow]{product.Stock}[/]");
    AnsiConsole.MarkupLine("[dim]Used: auditUnitOfWork.LogUpdateAsync(originalProduct, modifiedProduct)[/]\n");

    // Demo: LogDeleteAsync
    AnsiConsole.MarkupLine("[bold cyan]--- Demo: LogDeleteAsync ---[/]");
    product.IsActive = false;
    await dataOps.UpdateProductAsync(product);
    await auditUnitOfWork.LogDeleteAsync(product);

    AnsiConsole.MarkupLine($"Deleted Product ID: [yellow]{product.Id}[/]");
    AnsiConsole.MarkupLine("[dim]Used: auditUnitOfWork.LogDeleteAsync(product)[/]\n");

    // Show audit history
    AnsiConsole.MarkupLine("[bold cyan]--- Audit History for Product ---[/]");
    var entries = await auditService.GetAuditHistoryAsync("Product", product.Id.ToString());

    if (entries is not null && entries.Count > 0)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Timestamp[/]")
            .AddColumn("[bold]Action[/]")
            .AddColumn("[bold]User[/]")
            .AddColumn("[bold]Details[/]");

        foreach (var entry in entries)
        {
            string details;
            if (entry.Related is not null)
            {
                var fieldDetails = entry.Fields
                    .Select(f => f.Value is not null
                        ? $"{f.Name}: {f.Value}"
                        : $"{f.Name}: {f.Before} -> {f.After}")
                    .ToList();
                details = fieldDetails.Count > 0
                    ? $"{entry.Related} ({string.Join(", ", fieldDetails)})"
                    : $"Related: {entry.Related}";
            }
            else
            {
                details = string.Join(", ", entry.Fields
                    .Where(f => f.Before != null || f.After != null)
                    .Select(f => $"{f.Name}: {f.Before ?? "null"} -> {f.After ?? "null"}"));
            }

            table.AddRow(
                entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                entry.Action.ToString(),
                entry.User,
                string.IsNullOrEmpty(details) ? "-" : details);
        }

        AnsiConsole.Write(table);
    }

    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Spectre.Console.Rule("[green]IAuditUnitOfWork Demo completed![/]").RuleStyle("green"));
}

// ============================================================================
// User/Role/UserRole Demo (Identity-like scenario)
// ============================================================================
static async Task RunUserRoleDemo(
    IDemoDataOperations dataOps,
    IAuditUnitOfWork auditUnitOfWork,
    IAuditService auditService)
{
    // Cast to access User/Role/UserRole methods
    var dapperOps = (DapperDataOperations)dataOps;

    // Demo 8: Create Roles (not audited - just reference data)
    AnsiConsole.MarkupLine("[bold cyan]--- Demo 8: Create Roles ---[/]");

    var adminRole = new Role { RoleName = "Administrator", Description = "Full system access" };
    var userRole = new Role { RoleName = "User", Description = "Standard user access" };
    var guestRole = new Role { RoleName = "Guest", Description = "Limited read-only access" };

    await dapperOps.CreateRoleAsync(adminRole);
    await dapperOps.CreateRoleAsync(userRole);
    await dapperOps.CreateRoleAsync(guestRole);

    AnsiConsole.MarkupLine($"Created Roles: [yellow]Administrator[/], [yellow]User[/], [yellow]Guest[/]");
    AnsiConsole.MarkupLine("[dim]Roles are not audited (not configured as auditable entity)[/]\n");

    // Demo 9: Create User
    AnsiConsole.MarkupLine("[bold cyan]--- Demo 9: Create User with IAuditUnitOfWork ---[/]");

    var user = new User
    {
        UserName = "jane.doe",
        Email = "jane.doe@example.com",
        PhoneNumber = "+1-555-0456",
        IsActive = true
    };

    await dapperOps.CreateUserAsync(user);
    await auditUnitOfWork.LogCreateAsync(user);

    AnsiConsole.MarkupLine($"Created User: [yellow]{user.UserName}[/] ({user.Email})");
    AnsiConsole.MarkupLine($"User ID: [dim]{user.UserId}[/]");
    AnsiConsole.MarkupLine("[dim]Used: auditUnitOfWork.LogCreateAsync(user)[/]\n");

    // Demo 10: Assign Roles to User using LogRelatedAddedAsync with Lookups
    AnsiConsole.MarkupLine("[bold cyan]--- Demo 10: Assign Roles to User (with Lookups) ---[/]");
    AnsiConsole.MarkupLine("[dim]Using LogRelatedAddedAsync with lookup to show RoleName instead of RoleId[/]\n");

    // Assign Administrator role
    var adminAssignment = new UserRole { UserId = user.UserId, RoleId = adminRole.RoleId };
    await dapperOps.CreateUserRoleAsync(adminAssignment);
    // Pass the resolved Role entity as lookup - audit will capture RoleName
    await auditUnitOfWork.LogRelatedAddedAsync(user, adminAssignment, adminRole);

    AnsiConsole.MarkupLine($"Assigned role [yellow]Administrator[/]");
    AnsiConsole.MarkupLine("[dim]Used: auditUnitOfWork.LogRelatedAddedAsync(user, userRole, role)[/]");

    // Assign User role
    var userAssignment = new UserRole { UserId = user.UserId, RoleId = userRole.RoleId };
    await dapperOps.CreateUserRoleAsync(userAssignment);
    await auditUnitOfWork.LogRelatedAddedAsync(user, userAssignment, userRole);

    AnsiConsole.MarkupLine($"Assigned role [yellow]User[/]");
    AnsiConsole.MarkupLine("[green]Lookup captured 'RoleName' from the Role entity passed as parameter![/]\n");

    // Demo 11: Remove Role from User using LogRelatedRemovedAsync with Lookup
    AnsiConsole.MarkupLine("[bold cyan]--- Demo 11: Remove Role from User (with Lookup) ---[/]");

    await dapperOps.DeleteUserRoleAsync(user.UserId, userRole.RoleId);
    // Pass the Role entity to capture RoleName in the audit log
    await auditUnitOfWork.LogRelatedRemovedAsync(user, userAssignment, userRole);

    AnsiConsole.MarkupLine($"Removed role [yellow]User[/] from {user.UserName}");
    AnsiConsole.MarkupLine("[dim]Used: auditUnitOfWork.LogRelatedRemovedAsync(user, userRole, role)[/]\n");

    // Demo 12: Get User Audit History
    AnsiConsole.MarkupLine("[bold cyan]--- Demo 12: Get User Audit History ---[/]");
    var userEntries = await auditService.GetAuditHistoryAsync("User", user.UserId);

    if (userEntries is not null && userEntries.Count > 0)
    {
        var userTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Timestamp[/]")
            .AddColumn("[bold]Action[/]")
            .AddColumn("[bold]User[/]")
            .AddColumn("[bold]Details[/]");

        foreach (var entry in userEntries)
        {
            string details;
            if (entry.Related is not null)
            {
                var fieldDetails = entry.Fields
                    .Select(f => f.Value is not null
                        ? $"{f.Name}: {f.Value[..Math.Min(12, f.Value.Length)]}..."
                        : $"{f.Name}: {f.Before} -> {f.After}")
                    .ToList();
                details = fieldDetails.Count > 0
                    ? $"{entry.Related} ({string.Join(", ", fieldDetails)})"
                    : $"Related: {entry.Related}";
            }
            else
            {
                details = string.Join(", ", entry.Fields
                    .Where(f => f.Before != null || f.After != null)
                    .Select(f => $"{f.Name}: {f.Before ?? "null"} -> {f.After ?? "null"}"));
            }

            userTable.AddRow(
                entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                entry.Action.ToString(),
                entry.User,
                string.IsNullOrEmpty(details) ? "-" : details);
        }

        AnsiConsole.Write(userTable);

        AnsiConsole.MarkupLine("\n[green]Notice: With Lookups, the audit log shows 'RoleName: Administrator'[/]");
        AnsiConsole.MarkupLine("[green]instead of 'RoleId: guid...' - same as EF Core![/]");
    }

    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Spectre.Console.Rule("[green]User/Role Demo completed![/]").RuleStyle("green"));
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
