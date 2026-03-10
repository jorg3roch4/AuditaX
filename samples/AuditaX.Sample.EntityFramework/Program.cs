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

    // Demo 13: Pre-existing records (simulate entity without AuditLog)
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

    AnsiConsole.MarkupLine("[green]Pre-existing records now get audited on first action![/]");

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
