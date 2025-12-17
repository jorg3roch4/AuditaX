using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

var connectionString = config.GetConnectionString();
var tableName = config.GetAuditLogTableName("EF"); // EF = Entity Framework
var schema = config.GetSchema();

AnsiConsole.MarkupLine($"[dim]Database: {config.GetDatabaseName()}[/]");
AnsiConsole.MarkupLine($"[dim]AuditLog Table: {tableName}[/]");
AnsiConsole.MarkupLine($"[dim]Format: {config.Format}[/]");
AnsiConsole.MarkupLine($"[dim]Config Mode: {config.ConfigMode}[/]\n");

// Configure AuditaX first (registers the interceptor)
ConfigureAuditaX(builder.Services, builder.Configuration, config, tableName, schema);

// Register DbContext with AuditaX interceptor for automatic change tracking
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    if (config.Database == DatabaseType.SqlServer)
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }

    // Enable AuditaX for automatic audit logging
    options.UseAuditaX(sp);
});

var host = builder.Build();

try
{
    await host.StartAsync();

    using var scope = host.Services.CreateScope();
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<AppDbContext>();
    var auditService = services.GetRequiredService<IAuditService>();
    var auditQueryService = services.GetRequiredService<IAuditQueryService>();

    AnsiConsole.MarkupLine("[green]Database connected successfully.[/]");
    AnsiConsole.MarkupLine("[dim]Using automatic change tracking via EF Core interceptor.[/]\n");

    // Demo 1: Create Product (interceptor captures automatically)
    AnsiConsole.MarkupLine("[bold cyan]--- Demo 1: Create Product (Automatic) ---[/]");
    var product = new Product
    {
        Name = "Gaming Mouse RGB",
        Description = "High-precision gaming mouse with RGB lighting",
        Price = 79.99m,
        Stock = 100,
        IsActive = true
    };

    context.Products.Add(product);
    await context.SaveChangesAsync(); // Interceptor logs "Created" automatically

    AnsiConsole.MarkupLine($"Created Product ID: [yellow]{product.Id}[/]");
    AnsiConsole.MarkupLine("[dim]Audit logged automatically by interceptor.[/]\n");

    // Demo 2: Update Product (interceptor captures automatically)
    AnsiConsole.MarkupLine("[bold cyan]--- Demo 2: Update Product (Automatic) ---[/]");

    product.Price = 69.99m;
    product.Stock = 95;

    context.Products.Update(product);
    await context.SaveChangesAsync(); // Interceptor logs "Updated" with field changes

    AnsiConsole.MarkupLine($"Updated Product - Price: [yellow]{product.Price:C}[/], Stock: [yellow]{product.Stock}[/]");
    AnsiConsole.MarkupLine("[dim]Changes logged automatically by interceptor.[/]\n");

    // Demo 3: Add Related Entity (interceptor captures automatically)
    AnsiConsole.MarkupLine("[bold cyan]--- Demo 3: Add Related Entity (Automatic) ---[/]");

    var tag1 = new ProductTag { ProductId = product.Id, Tag = "Gaming" };
    context.ProductTags.Add(tag1);
    await context.SaveChangesAsync(); // Interceptor logs "Added" for related entity

    AnsiConsole.MarkupLine($"Added Tag: [yellow]{tag1.Tag}[/] (ID: {tag1.Id})");

    var tag2 = new ProductTag { ProductId = product.Id, Tag = "RGB" };
    context.ProductTags.Add(tag2);
    await context.SaveChangesAsync();

    AnsiConsole.MarkupLine($"Added Tag: [yellow]{tag2.Tag}[/] (ID: {tag2.Id})");
    AnsiConsole.MarkupLine("[dim]Related entity additions logged automatically.[/]\n");

    // Demo 4: Remove Related Entity (interceptor captures automatically)
    AnsiConsole.MarkupLine("[bold cyan]--- Demo 4: Remove Related Entity (Automatic) ---[/]");

    context.ProductTags.Remove(tag2);
    await context.SaveChangesAsync(); // Interceptor logs "Removed" for related entity

    AnsiConsole.MarkupLine($"Removed Tag: [yellow]{tag2.Tag}[/]");
    AnsiConsole.MarkupLine("[dim]Related entity removal logged automatically.[/]\n");

    // Demo 5: Soft Delete Product (interceptor captures automatically)
    AnsiConsole.MarkupLine("[bold cyan]--- Demo 5: Soft Delete Product (Automatic) ---[/]");
    product.IsActive = false;
    context.Products.Update(product);
    await context.SaveChangesAsync(); // Interceptor logs the IsActive change

    AnsiConsole.MarkupLine($"Soft deleted Product ID: [yellow]{product.Id}[/]");
    AnsiConsole.MarkupLine("[dim]Change logged automatically by interceptor.[/]\n");

    // Demo 6: Get Audit History
    AnsiConsole.MarkupLine("[bold cyan]--- Demo 6: Get Complete Audit History ---[/]");
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
    else
    {
        AnsiConsole.MarkupLine("[yellow]No audit history found.[/]");
    }

    // Demo 7: Query Audit Logs
    AnsiConsole.MarkupLine("\n[bold cyan]--- Demo 7: Query Audit Logs using IAuditQueryService ---[/]");

    AnsiConsole.MarkupLine("\n[dim]7.1: GetBySourceNameAsync('Product')[/]");
    var bySourceName = await auditQueryService.GetBySourceNameAsync("Product", skip: 0, take: 10);
    AnsiConsole.MarkupLine($"  Found [yellow]{bySourceName.Count()}[/] records for 'Product'");

    AnsiConsole.MarkupLine($"\n[dim]7.2: GetBySourceNameAndKeyAsync('Product', '{product.Id}')[/]");
    var bySourceNameAndKey = await auditQueryService.GetBySourceNameAndKeyAsync("Product", product.Id.ToString());
    if (bySourceNameAndKey != null)
    {
        AnsiConsole.MarkupLine($"  SourceName: [yellow]{bySourceNameAndKey.SourceName}[/]");
        AnsiConsole.MarkupLine($"  SourceKey: [yellow]{bySourceNameAndKey.SourceKey}[/]");
        AnsiConsole.MarkupLine($"  AuditLog length: [yellow]{bySourceNameAndKey.AuditLog.Length}[/] chars");
    }

    AnsiConsole.MarkupLine("\n[dim]7.3: GetBySourceNameAndActionAsync('Product', Created)[/]");
    var byAction = await auditQueryService.GetBySourceNameAndActionAsync("Product", AuditAction.Created);
    AnsiConsole.MarkupLine($"  Found [yellow]{byAction.Count()}[/] records with 'Created' action");

    AnsiConsole.MarkupLine("\n[dim]7.4: GetBySourceNameAndActionAsync('Product', Added)[/]");
    var byAddedAction = await auditQueryService.GetBySourceNameAndActionAsync("Product", AuditAction.Added);
    AnsiConsole.MarkupLine($"  Found [yellow]{byAddedAction.Count()}[/] records with 'Added' action (related entities)");

    AnsiConsole.MarkupLine("\n[dim]7.5: GetSummaryBySourceNameAsync('Product')[/]");
    var summary = await auditQueryService.GetSummaryBySourceNameAsync("Product", skip: 0, take: 100);
    foreach (var item in summary)
    {
        AnsiConsole.MarkupLine($"  {item.SourceName}[[{item.SourceKey}]]: Last [yellow]{item.LastAction}[/] at {item.LastTimestamp:yyyy-MM-dd HH:mm:ss} by {item.LastUser}");
    }

    // ============================================================================
    // Demo 8-11: User/Role/UserRole with Lookup (Identity-like scenario)
    // ============================================================================
    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule("[bold magenta]Identity-like Demo: User Roles with Lookup[/]").RuleStyle("magenta"));
    AnsiConsole.WriteLine();

    // Demo 8: Create Roles
    AnsiConsole.MarkupLine("[bold cyan]--- Demo 8: Create Roles ---[/]");

    var adminRole = new Role { RoleName = "Administrator", Description = "Full system access" };
    var userRole = new Role { RoleName = "User", Description = "Standard user access" };
    var guestRole = new Role { RoleName = "Guest", Description = "Limited read-only access" };

    context.Roles.AddRange(adminRole, userRole, guestRole);
    await context.SaveChangesAsync();

    AnsiConsole.MarkupLine($"Created Roles: [yellow]Administrator[/], [yellow]User[/], [yellow]Guest[/]");
    AnsiConsole.MarkupLine("[dim]Note: Roles are not audited (not configured as auditable entity)[/]\n");

    // Demo 9: Create User
    AnsiConsole.MarkupLine("[bold cyan]--- Demo 9: Create User (Automatic) ---[/]");

    var user = new User
    {
        UserName = "john.doe",
        Email = "john.doe@example.com",
        PhoneNumber = "+1-555-0123",
        IsActive = true
    };

    context.Users.Add(user);
    await context.SaveChangesAsync(); // Interceptor logs "Created" automatically

    AnsiConsole.MarkupLine($"Created User: [yellow]{user.UserName}[/] ({user.Email})");
    AnsiConsole.MarkupLine($"User ID: [dim]{user.UserId}[/]\n");

    // Demo 10: Assign Roles to User (Lookup resolves RoleName)
    AnsiConsole.MarkupLine("[bold cyan]--- Demo 10: Assign Roles to User (Lookup Demo) ---[/]");
    AnsiConsole.MarkupLine("[dim]When assigning roles, the audit log will show RoleName (from Roles table)[/]");
    AnsiConsole.MarkupLine("[dim]instead of just RoleId (from UserRoles table)[/]\n");

    // Assign Administrator role
    var adminAssignment = new UserRole { UserId = user.UserId, RoleId = adminRole.RoleId };
    context.UserRoles.Add(adminAssignment);
    await context.SaveChangesAsync(); // Interceptor logs "Added" with RoleName = "Administrator"

    AnsiConsole.MarkupLine($"Assigned role [yellow]Administrator[/] to {user.UserName}");

    // Assign User role
    var userAssignment = new UserRole { UserId = user.UserId, RoleId = userRole.RoleId };
    context.UserRoles.Add(userAssignment);
    await context.SaveChangesAsync(); // Interceptor logs "Added" with RoleName = "User"

    AnsiConsole.MarkupLine($"Assigned role [yellow]User[/] to {user.UserName}");
    AnsiConsole.MarkupLine("[dim]Lookup resolved RoleName from Roles table automatically![/]\n");

    // Demo 11: Remove Role from User (Lookup resolves RoleName)
    AnsiConsole.MarkupLine("[bold cyan]--- Demo 11: Remove Role from User (Lookup Demo) ---[/]");

    context.UserRoles.Remove(userAssignment);
    await context.SaveChangesAsync(); // Interceptor logs "Removed" with RoleName = "User"

    AnsiConsole.MarkupLine($"Removed role [yellow]User[/] from {user.UserName}");
    AnsiConsole.MarkupLine("[dim]Lookup resolved RoleName for removal too![/]\n");

    // Demo 12: Get User Audit History (shows Lookup values)
    AnsiConsole.MarkupLine("[bold cyan]--- Demo 12: Get User Audit History (with Lookup values) ---[/]");
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
                        ? $"{f.Name}: [yellow]{f.Value}[/]"
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

        AnsiConsole.MarkupLine("\n[green]Notice how the audit log shows 'RoleName: Administrator' instead of 'RoleId: guid...'[/]");
        AnsiConsole.MarkupLine("[green]This is the Lookup feature in action![/]");
    }

    // Demo 13: Show raw AuditLog content
    AnsiConsole.MarkupLine("\n[bold cyan]--- Demo 13: Raw AuditLog Content (from database) ---[/]");
    var rawAuditLog = await auditQueryService.GetBySourceNameAndKeyAsync("User", user.UserId);
    if (rawAuditLog is not null)
    {
        AnsiConsole.MarkupLine($"[dim]SourceName:[/] {Markup.Escape(rawAuditLog.SourceName)}");
        AnsiConsole.MarkupLine($"[dim]SourceKey:[/] {Markup.Escape(rawAuditLog.SourceKey)}");
        AnsiConsole.MarkupLine($"[dim]AuditLog ({rawAuditLog.AuditLog.Length} chars):[/]");

        var panel = new Panel(Markup.Escape(rawAuditLog.AuditLog))
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader($"[bold]{config.Format} Format[/]")
        };
        AnsiConsole.Write(panel);
    }

    // Demo 14: Pre-existing records (simulate entity without AuditLog)
    AnsiConsole.MarkupLine("\n[bold cyan]--- Demo 14: Pre-existing Records (no AuditLog) ---[/]");
    AnsiConsole.MarkupLine("[dim]Simulating a pre-existing User that was created before AuditaX was enabled[/]");

    // Delete the existing AuditLog to simulate a pre-existing record
    var existingAuditLog = await context.Set<AuditaX.Entities.AuditLog>()
        .FirstOrDefaultAsync(a => a.SourceName == "User" && a.SourceKey == user.UserId);
    if (existingAuditLog is not null)
    {
        context.Set<AuditaX.Entities.AuditLog>().Remove(existingAuditLog);
        await context.SaveChangesAsync();
        AnsiConsole.MarkupLine("[dim]Deleted existing AuditLog to simulate pre-existing record[/]");
    }

    // Now add a role to this "pre-existing" user - this should create a new AuditLog
    var guestAssignment = new UserRole { UserId = user.UserId, RoleId = guestRole.RoleId };
    context.UserRoles.Add(guestAssignment);
    await context.SaveChangesAsync();

    AnsiConsole.MarkupLine($"Added role [yellow]Guest[/] to pre-existing user");

    // Verify that AuditLog was created
    var newUserEntries = await auditService.GetAuditHistoryAsync("User", user.UserId);
    if (newUserEntries is not null && newUserEntries.Count > 0)
    {
        AnsiConsole.MarkupLine($"[green]AuditLog created with {newUserEntries.Count} entry(ies)![/]");

        var preExistingTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Action[/]")
            .AddColumn("[bold]Related[/]")
            .AddColumn("[bold]Details[/]");

        foreach (var entry in newUserEntries)
        {
            var details = string.Join(", ", entry.Fields.Select(f =>
                f.Value is not null ? $"{f.Name}: {f.Value}" : $"{f.Name}: {f.Before} -> {f.After}"));
            preExistingTable.AddRow(
                entry.Action.ToString(),
                entry.Related ?? "-",
                details);
        }

        AnsiConsole.Write(preExistingTable);
        AnsiConsole.MarkupLine("\n[green]Pre-existing records now get audited on first action![/]");
    }
    else
    {
        AnsiConsole.MarkupLine("[red]ERROR: AuditLog was NOT created for pre-existing record![/]");
    }

    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule("[green]Demo completed successfully![/]").RuleStyle("green"));
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

            // Product configuration (existing)
            options.ConfigureEntity<Product>("Product")
                .WithKey(p => p.Id)
                .Properties("Name", "Description", "Price", "Stock", "IsActive")
                .WithRelatedEntity<ProductTag>("ProductTag")
                    .WithParentKey(t => t.ProductId)
                    .Properties("Tag");

            // User configuration with Lookup (NEW - Identity-like scenario)
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

    // Extensible API: UseEntityFramework + UseSqlServer/UsePostgreSql
    builder
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
        Format = fmt == "xml" ? LogFormat.Xml : LogFormat.Json,
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
