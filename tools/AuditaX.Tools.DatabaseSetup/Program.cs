using Microsoft.Data.SqlClient;
using Npgsql;
using Spectre.Console;

namespace AuditaX.Tools.DatabaseSetup;

/// <summary>
/// AuditaX Database Setup Tool
/// Creates sample databases for SQL Server and PostgreSQL
/// Executes scripts from the scripts/ folder (00-05 only, not 99_* alternative scripts)
/// </summary>
public static class Program
{
    // SQL Server configuration
    private const string SqlServerMasterConnectionString = "Server=localhost;Database=master;User Id=sa;Password=sa;TrustServerCertificate=True;";
    private const string SqlServerDatabaseConnectionString = "Server=localhost;Database=AuditaX;User Id=sa;Password=sa;TrustServerCertificate=True;";
    private const string SqlServerDatabaseName = "AuditaX";

    // PostgreSQL configuration
    private const string PostgreSqlMasterConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres;";
    private const string PostgreSqlDatabaseConnectionString = "Host=localhost;Port=5432;Database=auditax;Username=postgres;Password=postgres;";
    private const string PostgreSqlDatabaseName = "auditax";

    // Scripts folder path
    private static string ScriptsPath = null!;

    public static async Task<int> Main(string[] args)
    {
        ScriptsPath = FindScriptsPath();

        AnsiConsole.Write(
            new FigletText("AuditaX")
                .LeftJustified()
                .Color(Color.Blue));

        AnsiConsole.MarkupLine("[bold blue]Database Setup Tool[/]");
        AnsiConsole.MarkupLine("[dim]Creates sample databases for AuditaX samples[/]");
        AnsiConsole.MarkupLine($"[dim]Scripts path: {ScriptsPath}[/]\n");

        if (!Directory.Exists(ScriptsPath))
        {
            AnsiConsole.MarkupLine($"[red]Error: Scripts folder not found at {ScriptsPath}[/]");
            AnsiConsole.MarkupLine("[yellow]Make sure you run this tool from the AuditaX repository[/]");
            return 1;
        }

        // Support command-line arguments for non-interactive mode
        // Usage: dotnet run -- [all|sqlserver|postgresql]
        string choice;
        if (args.Length > 0)
        {
            choice = args[0].ToLowerInvariant() switch
            {
                "all" => "Create all databases (SQL Server + PostgreSQL)",
                "sqlserver" or "sql" => "Create SQL Server database only",
                "postgresql" or "pg" => "Create PostgreSQL database only",
                _ => "Exit"
            };
            AnsiConsole.MarkupLine($"[dim]Running in non-interactive mode: {choice}[/]\n");
        }
        else
        {
            choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]What would you like to do?[/]")
                    .PageSize(10)
                    .AddChoices(
                    [
                        "Create all databases (SQL Server + PostgreSQL)",
                        "Create SQL Server database only",
                        "Create PostgreSQL database only",
                        "Exit"
                    ]));
        }

        var result = 0;

        switch (choice)
        {
            case "Create all databases (SQL Server + PostgreSQL)":
                result = await CreateAllDatabasesAsync();
                break;
            case "Create SQL Server database only":
                result = await CreateSqlServerDatabaseAsync();
                break;
            case "Create PostgreSQL database only":
                result = await CreatePostgreSqlDatabaseAsync();
                break;
            case "Exit":
                AnsiConsole.MarkupLine("[dim]Goodbye![/]");
                return 0;
        }

        if (result == 0)
        {
            PrintSummary();
        }

        return result;
    }

    private static string FindScriptsPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);

        while (dir != null)
        {
            var scriptsDir = Path.Combine(dir.FullName, "scripts");
            if (Directory.Exists(Path.Combine(scriptsDir, "SqlServer")))
            {
                return scriptsDir;
            }

            dir = dir.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "scripts");
    }

    private static async Task<int> CreateAllDatabasesAsync()
    {
        var sqlResult = await CreateSqlServerDatabaseAsync();
        AnsiConsole.WriteLine();
        var pgResult = await CreatePostgreSqlDatabaseAsync();

        return sqlResult != 0 ? sqlResult : pgResult;
    }

    private static async Task<int> CreateSqlServerDatabaseAsync()
    {
        AnsiConsole.MarkupLine("\n[bold cyan]SQL Server Database[/]");
        AnsiConsole.MarkupLine($"[dim]Database: {SqlServerDatabaseName}[/]\n");

        try
        {
            // Step 1: Create database using master connection
            await using (var masterConnection = new SqlConnection(SqlServerMasterConnectionString))
            {
                await masterConnection.OpenAsync();

                // Drop if exists
                var dropSql = $@"
                    IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{SqlServerDatabaseName}')
                    BEGIN
                        ALTER DATABASE [{SqlServerDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE [{SqlServerDatabaseName}];
                    END";

                await using (var dropCmd = new SqlCommand(dropSql, masterConnection))
                {
                    await dropCmd.ExecuteNonQueryAsync();
                }

                // Create database
                var createSql = $"CREATE DATABASE [{SqlServerDatabaseName}]";
                await using (var createCmd = new SqlCommand(createSql, masterConnection))
                {
                    await createCmd.ExecuteNonQueryAsync();
                }

                AnsiConsole.MarkupLine($"[green]Database {SqlServerDatabaseName} created.[/]");
            }

            // Step 2: Execute table scripts (01-05 only, not 99_*)
            await using var dbConnection = new SqlConnection(SqlServerDatabaseConnectionString);
            await dbConnection.OpenAsync();

            var scriptsFolder = Path.Combine(ScriptsPath, "SqlServer");
            var scripts = Directory.GetFiles(scriptsFolder, "*.sql")
                .Where(f => !Path.GetFileName(f).StartsWith("00_") && !Path.GetFileName(f).StartsWith("99_"))
                .OrderBy(f => f)
                .ToList();

            await AnsiConsole.Progress()
                .AutoClear(false)
                .Columns(
                [
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn()
                ])
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[cyan]Creating tables[/]", maxValue: scripts.Count);

                    foreach (var scriptPath in scripts)
                    {
                        var scriptName = Path.GetFileName(scriptPath);
                        task.Description = $"[cyan]Executing {scriptName}[/]";

                        var script = await File.ReadAllTextAsync(scriptPath);
                        script = RemoveSqlServerUseStatement(script);
                        await ExecuteSqlServerBatchesAsync(dbConnection, script);

                        task.Increment(1);
                    }

                    task.Description = "[green]Tables created[/]";
                });

            // Show results
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Table[/]")
                .AddColumn("[bold]Status[/]");

            table.AddRow("Products", "[green]Created[/]");
            table.AddRow("ProductTags", "[green]Created[/]");
            table.AddRow("Users", "[green]Created[/]");
            table.AddRow("Roles", "[green]Created[/]");
            table.AddRow("UserRoles", "[green]Created[/]");

            AnsiConsole.Write(table);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            AnsiConsole.MarkupLine("[yellow]Make sure SQL Server is running on localhost with sa/sa credentials[/]");
            return 1;
        }
    }

    private static string RemoveSqlServerUseStatement(string script)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            script,
            @"USE\s+\[[\w]+\];\s*GO",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static async Task ExecuteSqlServerBatchesAsync(SqlConnection connection, string script)
    {
        var batches = System.Text.RegularExpressions.Regex.Split(
            script,
            @"^\s*GO\s*$",
            System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (var batch in batches)
        {
            var trimmedBatch = batch.Trim();
            if (string.IsNullOrWhiteSpace(trimmedBatch))
                continue;

            if (trimmedBatch.StartsWith("PRINT", StringComparison.OrdinalIgnoreCase))
                continue;

            await using var cmd = new SqlCommand(trimmedBatch, connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task<int> CreatePostgreSqlDatabaseAsync()
    {
        AnsiConsole.MarkupLine("\n[bold green]PostgreSQL Database[/]");
        AnsiConsole.MarkupLine($"[dim]Database: {PostgreSqlDatabaseName}[/]\n");

        try
        {
            // Step 1: Create database using postgres connection
            await using (var postgresConnection = new NpgsqlConnection(PostgreSqlMasterConnectionString))
            {
                await postgresConnection.OpenAsync();

                // Terminate existing connections
                try
                {
                    var terminateSql = $@"
                        SELECT pg_terminate_backend(pg_stat_activity.pid)
                        FROM pg_stat_activity
                        WHERE pg_stat_activity.datname = '{PostgreSqlDatabaseName}'
                        AND pid <> pg_backend_pid();";

                    await using var terminateCmd = new NpgsqlCommand(terminateSql, postgresConnection);
                    await terminateCmd.ExecuteNonQueryAsync();
                }
                catch { /* Ignore if database doesn't exist */ }

                // Drop if exists
                try
                {
                    var dropSql = $"DROP DATABASE IF EXISTS {PostgreSqlDatabaseName};";
                    await using var dropCmd = new NpgsqlCommand(dropSql, postgresConnection);
                    await dropCmd.ExecuteNonQueryAsync();
                }
                catch { /* Ignore */ }

                // Create database
                var createSql = $"CREATE DATABASE {PostgreSqlDatabaseName};";
                await using (var createCmd = new NpgsqlCommand(createSql, postgresConnection))
                {
                    await createCmd.ExecuteNonQueryAsync();
                }

                AnsiConsole.MarkupLine($"[green]Database {PostgreSqlDatabaseName} created.[/]");
            }

            // Step 2: Execute table scripts (01-05 only, not 99_*)
            await using var dbConnection = new NpgsqlConnection(PostgreSqlDatabaseConnectionString);
            await dbConnection.OpenAsync();

            var scriptsFolder = Path.Combine(ScriptsPath, "PostgreSQL");
            var scripts = Directory.GetFiles(scriptsFolder, "*.sql")
                .Where(f => !Path.GetFileName(f).StartsWith("00_") && !Path.GetFileName(f).StartsWith("99_"))
                .OrderBy(f => f)
                .ToList();

            await AnsiConsole.Progress()
                .AutoClear(false)
                .Columns(
                [
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn()
                ])
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Creating tables[/]", maxValue: scripts.Count);

                    foreach (var scriptPath in scripts)
                    {
                        var scriptName = Path.GetFileName(scriptPath);
                        task.Description = $"[green]Executing {scriptName}[/]";

                        var script = await File.ReadAllTextAsync(scriptPath);
                        await using var cmd = new NpgsqlCommand(script, dbConnection);
                        await cmd.ExecuteNonQueryAsync();

                        task.Increment(1);
                    }

                    task.Description = "[green]Tables created[/]";
                });

            // Show results
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Table[/]")
                .AddColumn("[bold]Status[/]");

            table.AddRow("products", "[green]Created[/]");
            table.AddRow("product_tags", "[green]Created[/]");
            table.AddRow("users", "[green]Created[/]");
            table.AddRow("roles", "[green]Created[/]");
            table.AddRow("user_roles", "[green]Created[/]");

            AnsiConsole.Write(table);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            AnsiConsole.MarkupLine("[yellow]Make sure PostgreSQL is running on localhost:5432 with postgres/postgres credentials[/]");
            return 1;
        }
    }

    private static void PrintSummary()
    {
        AnsiConsole.WriteLine();

        var panel = new Panel(
            new Markup(
                "[bold]Databases:[/]\n" +
                "[cyan]SQL Server:[/] AuditaX\n" +
                "[green]PostgreSQL:[/] auditax\n\n" +
                "[bold]Tables Created:[/]\n" +
                "  Products, ProductTags\n" +
                "  Users, Roles, UserRoles\n\n" +
                "[bold]AuditLog Tables:[/]\n" +
                "  Created automatically by AuditaX (AutoCreateTable=true)\n" +
                "  Or use scripts 99_* for production environments\n\n" +
                "[dim]AuditLog table names by sample:[/]\n" +
                "  Dapper JSON:  [cyan]AuditLogDJ[/] / [green]audit_log_dj[/]\n" +
                "  Dapper XML:   [cyan]AuditLogDX[/] / [green]audit_log_dx[/]\n" +
                "  EF Core JSON: [cyan]AuditLogEFJ[/] / [green]audit_log_efj[/]\n" +
                "  EF Core XML:  [cyan]AuditLogEFX[/] / [green]audit_log_efx[/]"))
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader("[bold blue] Summary [/]")
        };

        AnsiConsole.Write(panel);

        AnsiConsole.MarkupLine("\n[bold green]Setup completed successfully![/]");
        AnsiConsole.MarkupLine("[dim]You can now run the AuditaX sample projects.[/]");
    }
}
