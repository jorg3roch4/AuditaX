using Microsoft.Data.SqlClient;
using Npgsql;
using Spectre.Console;

namespace AuditaX.Tools.DatabaseSetup;

/// <summary>
/// AuditaX Database Setup Tool
/// Creates all sample databases for SQL Server and PostgreSQL
/// Reads SQL scripts from the scripts/ folder
/// </summary>
public static class Program
{
    // SQL Server configuration
    private const string SqlServerConnectionString = "Server=localhost;Database=master;User Id=sa;Password=sa;TrustServerCertificate=True;";

    private static readonly DatabaseInfo[] SqlServerDatabases =
    [
        new("AuditaXDapperSqlServerJson", "Dapper + SQL Server + JSON format", "Dapper.Json"),
        new("AuditaXDapperSqlServerXml", "Dapper + SQL Server + XML format", "Dapper.Xml"),
        new("AuditaXEFSqlServerJson", "Entity Framework + SQL Server + JSON format", "EF.Json"),
        new("AuditaXEFSqlServerXml", "Entity Framework + SQL Server + XML format", "EF.Xml")
    ];

    // PostgreSQL configuration
    private const string PostgreSqlConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres;";

    private static readonly DatabaseInfo[] PostgreSqlDatabases =
    [
        new("auditax_dapper_postgresql_json", "Dapper + PostgreSQL + JSON format", "Dapper.Json"),
        new("auditax_dapper_postgresql_xml", "Dapper + PostgreSQL + XML format", "Dapper.Xml"),
        new("auditax_ef_postgresql_json", "Entity Framework + PostgreSQL + JSON format", "EF.Json"),
        new("auditax_ef_postgresql_xml", "Entity Framework + PostgreSQL + XML format", "EF.Xml")
    ];

    // Scripts folder path (relative to tool location)
    private static string ScriptsPath = null!;

    public static async Task<int> Main(string[] args)
    {
        // Determine scripts path
        ScriptsPath = FindScriptsPath();

        AnsiConsole.Write(
            new FigletText("AuditaX")
                .LeftJustified()
                .Color(Color.Blue));

        AnsiConsole.MarkupLine("[bold blue]Database Setup Tool[/]");
        AnsiConsole.MarkupLine("[dim]Creates all sample databases for AuditaX samples[/]");
        AnsiConsole.MarkupLine($"[dim]Scripts path: {ScriptsPath}[/]\n");

        if (!Directory.Exists(ScriptsPath))
        {
            AnsiConsole.MarkupLine($"[red]Error: Scripts folder not found at {ScriptsPath}[/]");
            AnsiConsole.MarkupLine("[yellow]Make sure you run this tool from the AuditaX repository[/]");
            return 1;
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]What would you like to do?[/]")
                .PageSize(10)
                .AddChoices(
                [
                    "Create all databases (SQL Server + PostgreSQL)",
                    "Create SQL Server databases only",
                    "Create PostgreSQL databases only",
                    "Exit"
                ]));

        var result = 0;

        switch (choice)
        {
            case "Create all databases (SQL Server + PostgreSQL)":
                result = await CreateAllDatabasesAsync();
                break;
            case "Create SQL Server databases only":
                result = await CreateSqlServerDatabasesAsync();
                break;
            case "Create PostgreSQL databases only":
                result = await CreatePostgreSqlDatabasesAsync();
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
        // Try to find scripts folder relative to the executable
        var baseDir = AppContext.BaseDirectory;

        // Navigate up to find the repository root
        var dir = new DirectoryInfo(baseDir);
        while (dir != null)
        {
            var scriptsDir = Path.Combine(dir.FullName, "scripts", "Samples");
            if (Directory.Exists(scriptsDir))
            {
                return scriptsDir;
            }

            // Also check if we're in the tools folder structure
            scriptsDir = Path.Combine(dir.FullName, "..", "..", "scripts", "Samples");
            if (Directory.Exists(scriptsDir))
            {
                return Path.GetFullPath(scriptsDir);
            }

            dir = dir.Parent;
        }

        // Fallback: assume we're running from repository root
        return Path.Combine(Directory.GetCurrentDirectory(), "scripts", "Samples");
    }

    private static async Task<int> CreateAllDatabasesAsync()
    {
        var sqlResult = await CreateSqlServerDatabasesAsync();
        AnsiConsole.WriteLine();
        var pgResult = await CreatePostgreSqlDatabasesAsync();

        return sqlResult != 0 ? sqlResult : pgResult;
    }

    private static async Task<int> CreateSqlServerDatabasesAsync()
    {
        AnsiConsole.MarkupLine("\n[bold cyan]SQL Server Databases[/]");
        AnsiConsole.MarkupLine("[dim]Connection: localhost (sa/sa)[/]\n");

        try
        {
            await using var masterConnection = new SqlConnection(SqlServerConnectionString);
            await masterConnection.OpenAsync();

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
                    var task = ctx.AddTask("[cyan]Creating SQL Server databases[/]", maxValue: SqlServerDatabases.Length);

                    foreach (var db in SqlServerDatabases)
                    {
                        task.Description = $"[cyan]Creating {db.Name}[/]";
                        await CreateSqlServerDatabaseAsync(masterConnection, db);
                        task.Increment(1);
                    }

                    task.Description = "[green]SQL Server databases created[/]";
                });

            // Show created databases
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Database[/]")
                .AddColumn("[bold]Description[/]")
                .AddColumn("[bold]Status[/]");

            foreach (var db in SqlServerDatabases)
            {
                table.AddRow(
                    $"[cyan]{db.Name}[/]",
                    db.Description,
                    "[green]Created[/]");
            }

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

    private static async Task CreateSqlServerDatabaseAsync(SqlConnection masterConnection, DatabaseInfo db)
    {
        // Drop if exists
        var dropSql = $@"
            IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{db.Name}')
            BEGIN
                ALTER DATABASE [{db.Name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{db.Name}];
            END";

        await using (var dropCmd = new SqlCommand(dropSql, masterConnection))
        {
            await dropCmd.ExecuteNonQueryAsync();
        }

        // Create database
        var createSql = $"CREATE DATABASE [{db.Name}]";
        await using (var createCmd = new SqlCommand(createSql, masterConnection))
        {
            await createCmd.ExecuteNonQueryAsync();
        }

        // Connect to new database and create tables
        var dbConnectionString = $"Server=localhost;Database={db.Name};User Id=sa;Password=sa;TrustServerCertificate=True;";
        await using var dbConnection = new SqlConnection(dbConnectionString);
        await dbConnection.OpenAsync();

        // Read and execute tables script from file
        var tablesScriptPath = Path.Combine(ScriptsPath, "SqlServer", db.ScriptFolder, "02_CreateTables.sql");
        var tablesSql = await ReadAndPrepareSqlServerScript(tablesScriptPath, db.Name);

        await ExecuteSqlServerBatchesAsync(dbConnection, tablesSql);
    }

    private static async Task<string> ReadAndPrepareSqlServerScript(string scriptPath, string databaseName)
    {
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Script not found: {scriptPath}");
        }

        var script = await File.ReadAllTextAsync(scriptPath);

        // Remove USE statement (we're already connected to the right database)
        script = System.Text.RegularExpressions.Regex.Replace(
            script,
            @"USE\s+\[[\w]+\];\s*GO",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return script;
    }

    private static async Task ExecuteSqlServerBatchesAsync(SqlConnection connection, string script)
    {
        // Split by GO statements
        var batches = System.Text.RegularExpressions.Regex.Split(
            script,
            @"^\s*GO\s*$",
            System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (var batch in batches)
        {
            var trimmedBatch = batch.Trim();
            if (string.IsNullOrWhiteSpace(trimmedBatch))
                continue;

            // Skip PRINT statements for cleaner execution
            if (trimmedBatch.StartsWith("PRINT", StringComparison.OrdinalIgnoreCase))
                continue;

            await using var cmd = new SqlCommand(trimmedBatch, connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task<int> CreatePostgreSqlDatabasesAsync()
    {
        AnsiConsole.MarkupLine("\n[bold green]PostgreSQL Databases[/]");
        AnsiConsole.MarkupLine("[dim]Connection: localhost:5432 (postgres/postgres)[/]\n");

        try
        {
            await using var postgresConnection = new NpgsqlConnection(PostgreSqlConnectionString);
            await postgresConnection.OpenAsync();

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
                    var task = ctx.AddTask("[green]Creating PostgreSQL databases[/]", maxValue: PostgreSqlDatabases.Length);

                    foreach (var db in PostgreSqlDatabases)
                    {
                        task.Description = $"[green]Creating {db.Name}[/]";
                        await CreatePostgreSqlDatabaseAsync(postgresConnection, db);
                        task.Increment(1);
                    }

                    task.Description = "[green]PostgreSQL databases created[/]";
                });

            // Show created databases
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Database[/]")
                .AddColumn("[bold]Description[/]")
                .AddColumn("[bold]Status[/]");

            foreach (var db in PostgreSqlDatabases)
            {
                table.AddRow(
                    $"[green]{db.Name}[/]",
                    db.Description,
                    "[green]Created[/]");
            }

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

    private static async Task CreatePostgreSqlDatabaseAsync(NpgsqlConnection postgresConnection, DatabaseInfo db)
    {
        // Terminate existing connections
        try
        {
            var terminateSql = $@"
                SELECT pg_terminate_backend(pg_stat_activity.pid)
                FROM pg_stat_activity
                WHERE pg_stat_activity.datname = '{db.Name}'
                AND pid <> pg_backend_pid();";

            await using var terminateCmd = new NpgsqlCommand(terminateSql, postgresConnection);
            await terminateCmd.ExecuteNonQueryAsync();
        }
        catch { /* Ignore if database doesn't exist */ }

        // Drop if exists
        try
        {
            var dropSql = $"DROP DATABASE IF EXISTS {db.Name};";
            await using var dropCmd = new NpgsqlCommand(dropSql, postgresConnection);
            await dropCmd.ExecuteNonQueryAsync();
        }
        catch { /* Ignore */ }

        // Create database
        var createSql = $"CREATE DATABASE {db.Name};";
        await using (var createCmd = new NpgsqlCommand(createSql, postgresConnection))
        {
            await createCmd.ExecuteNonQueryAsync();
        }

        // Connect to new database and create tables
        var dbConnectionString = $"Host=localhost;Port=5432;Database={db.Name};Username=postgres;Password=postgres;";
        await using var dbConnection = new NpgsqlConnection(dbConnectionString);
        await dbConnection.OpenAsync();

        // Read and execute tables script from file
        var tablesScriptPath = Path.Combine(ScriptsPath, "PostgreSql", db.ScriptFolder, "02_create_tables.sql");
        var tablesSql = await ReadAndPreparePostgreSqlScript(tablesScriptPath);

        await using var tablesCmd = new NpgsqlCommand(tablesSql, dbConnection);
        await tablesCmd.ExecuteNonQueryAsync();
    }

    private static async Task<string> ReadAndPreparePostgreSqlScript(string scriptPath)
    {
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Script not found: {scriptPath}");
        }

        var script = await File.ReadAllTextAsync(scriptPath);

        // Remove connection comments
        script = System.Text.RegularExpressions.Regex.Replace(
            script,
            @"--\s*Connect to.*",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove psql commands
        script = System.Text.RegularExpressions.Regex.Replace(
            script,
            @"--\s*psql\s+-.*",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return script;
    }

    private static void PrintSummary()
    {
        AnsiConsole.WriteLine();

        var panel = new Panel(
            new Markup(
                "[bold]Database Schema:[/]\n\n" +
                "[cyan]Products[/] (Id, Name, Description, Price, Stock, IsActive)\n" +
                "[cyan]ProductTags[/] (Id, ProductId, Tag) -> FK to Products\n\n" +
                "[dim]Note: AuditLog table will be created automatically by AuditaX[/]\n" +
                "[dim]when AutoCreateTable = true in configuration[/]\n\n" +
                $"[dim]Scripts loaded from: {ScriptsPath}[/]"))
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader("[bold blue] Summary [/]")
        };

        AnsiConsole.Write(panel);

        AnsiConsole.MarkupLine("\n[bold green]Setup completed successfully![/]");
        AnsiConsole.MarkupLine("[dim]You can now run the AuditaX sample projects.[/]");
    }

    private record DatabaseInfo(string Name, string Description, string ScriptFolder);
}
