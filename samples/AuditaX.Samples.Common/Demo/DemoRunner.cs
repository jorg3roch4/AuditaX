using AuditaX.Enums;
using AuditaX.Interfaces;
using AuditaX.Models;
using AuditaX.Samples.Common.Entities;
using Spectre.Console;

namespace AuditaX.Samples.Common.Demo;

/// <summary>
/// Runs the standard AuditaX demo scenarios.
/// This class contains all the demo logic shared across samples.
/// </summary>
public class DemoRunner
{
    private readonly IAuditService _auditService;
    private readonly IAuditQueryService _auditQueryService;

    public DemoRunner(IAuditService auditService, IAuditQueryService auditQueryService)
    {
        _auditService = auditService;
        _auditQueryService = auditQueryService;
    }

    /// <summary>
    /// Runs all demo scenarios using the provided data operations.
    /// </summary>
    public async Task RunAsync(IDemoDataOperations dataOps)
    {
        AnsiConsole.MarkupLine("[green]Database connected successfully.[/]\n");

        // Demo 1: Create Product
        AnsiConsole.MarkupLine("[bold cyan]--- Demo 1: Create Product ---[/]");
        var product = new Product
        {
            Name = "Gaming Mouse RGB",
            Description = "High-precision gaming mouse with RGB lighting",
            Price = 79.99m,
            Stock = 100,
            IsActive = true
        };

        product = await dataOps.CreateProductAsync(product);
        await _auditService.LogCreateAsync("Product", product.Id.ToString());

        AnsiConsole.MarkupLine($"Created Product ID: [yellow]{product.Id}[/]");
        AnsiConsole.MarkupLine("[dim]Audit logged to centralized AuditLog table.[/]\n");

        // Demo 2: Update Product
        AnsiConsole.MarkupLine("[bold cyan]--- Demo 2: Update Product ---[/]");
        var originalPrice = product.Price;
        var originalStock = product.Stock;

        product.Price = 69.99m;
        product.Stock = 95;

        await dataOps.UpdateProductAsync(product);

        List<FieldChange> changes =
        [
            new() { Name = "Price", Before = originalPrice.ToString(), After = product.Price.ToString() },
            new() { Name = "Stock", Before = originalStock.ToString(), After = product.Stock.ToString() }
        ];
        await _auditService.LogUpdateAsync("Product", product.Id.ToString(), changes);

        AnsiConsole.MarkupLine($"Updated Product - Price: [yellow]{product.Price:C}[/], Stock: [yellow]{product.Stock}[/]");
        AnsiConsole.MarkupLine("[dim]Changes logged to centralized AuditLog table.[/]\n");

        // Demo 3: Add Related Entity (ProductTag)
        AnsiConsole.MarkupLine("[bold cyan]--- Demo 3: Add Related Entity (ProductTag) ---[/]");

        var tag1 = new ProductTag { ProductId = product.Id, Tag = "Gaming" };
        tag1 = await dataOps.CreateProductTagAsync(tag1);

        await _auditService.LogRelatedAsync(
            "Product",
            product.Id.ToString(),
            AuditAction.Added,
            "ProductTag",
            [new FieldChange { Name = "Tag", After = tag1.Tag }]);

        AnsiConsole.MarkupLine($"Added Tag: [yellow]{tag1.Tag}[/] (ID: {tag1.Id})");

        var tag2 = new ProductTag { ProductId = product.Id, Tag = "RGB" };
        tag2 = await dataOps.CreateProductTagAsync(tag2);

        await _auditService.LogRelatedAsync(
            "Product",
            product.Id.ToString(),
            AuditAction.Added,
            "ProductTag",
            [new FieldChange { Name = "Tag", After = tag2.Tag }]);

        AnsiConsole.MarkupLine($"Added Tag: [yellow]{tag2.Tag}[/] (ID: {tag2.Id})");
        AnsiConsole.MarkupLine("[dim]Related entity additions logged.[/]\n");

        // Demo 4: Remove Related Entity
        AnsiConsole.MarkupLine("[bold cyan]--- Demo 4: Remove Related Entity (ProductTag) ---[/]");

        await dataOps.DeleteProductTagAsync(tag2.Id);

        await _auditService.LogRelatedAsync(
            "Product",
            product.Id.ToString(),
            AuditAction.Removed,
            "ProductTag",
            [new FieldChange { Name = "Tag", Before = tag2.Tag }]);

        AnsiConsole.MarkupLine($"Removed Tag: [yellow]{tag2.Tag}[/]");
        AnsiConsole.MarkupLine("[dim]Related entity removal logged.[/]\n");

        // Demo 5: Soft Delete Product
        AnsiConsole.MarkupLine("[bold cyan]--- Demo 5: Soft Delete Product ---[/]");
        product.IsActive = false;
        await dataOps.UpdateProductAsync(product);
        await _auditService.LogDeleteAsync("Product", product.Id.ToString());

        AnsiConsole.MarkupLine($"Soft deleted Product ID: [yellow]{product.Id}[/]");
        AnsiConsole.MarkupLine("[dim]Delete logged to centralized AuditLog table.[/]\n");

        // Demo 6: Get Audit History
        AnsiConsole.MarkupLine("[bold cyan]--- Demo 6: Get Complete Audit History ---[/]");
        var entries = await _auditService.GetAuditHistoryAsync("Product", product.Id.ToString());

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
                var details = entry.Related is not null
                    ? $"Related: {entry.Related}"
                    : string.Join(", ", entry.Fields
                        .Where(f => f.Before != null || f.After != null)
                        .Select(f => $"{f.Name}: {f.Before ?? "null"} -> {f.After ?? "null"}"));

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
        var bySourceName = await _auditQueryService.GetBySourceNameAsync("Product", skip: 0, take: 10);
        AnsiConsole.MarkupLine($"  Found [yellow]{bySourceName.Count()}[/] records for 'Product'");

        AnsiConsole.MarkupLine($"\n[dim]7.2: GetBySourceNameAndKeyAsync('Product', '{product.Id}')[/]");
        var bySourceNameAndKey = await _auditQueryService.GetBySourceNameAndKeyAsync("Product", product.Id.ToString());
        if (bySourceNameAndKey != null)
        {
            AnsiConsole.MarkupLine($"  SourceName: [yellow]{bySourceNameAndKey.SourceName}[/]");
            AnsiConsole.MarkupLine($"  SourceKey: [yellow]{bySourceNameAndKey.SourceKey}[/]");
            AnsiConsole.MarkupLine($"  AuditLog length: [yellow]{bySourceNameAndKey.AuditLog.Length}[/] chars");
        }

        AnsiConsole.MarkupLine("\n[dim]7.3: GetBySourceNameAndActionAsync('Product', Created)[/]");
        var byAction = await _auditQueryService.GetBySourceNameAndActionAsync("Product", AuditAction.Created);
        AnsiConsole.MarkupLine($"  Found [yellow]{byAction.Count()}[/] records with 'Created' action");

        AnsiConsole.MarkupLine("\n[dim]7.4: GetBySourceNameAndActionAsync('Product', Added)[/]");
        var byAddedAction = await _auditQueryService.GetBySourceNameAndActionAsync("Product", AuditAction.Added);
        AnsiConsole.MarkupLine($"  Found [yellow]{byAddedAction.Count()}[/] records with 'Added' action (related entities)");

        AnsiConsole.MarkupLine("\n[dim]7.5: GetSummaryBySourceNameAsync('Product')[/]");
        var summary = await _auditQueryService.GetSummaryBySourceNameAsync("Product", skip: 0, take: 100);
        foreach (var item in summary)
        {
            AnsiConsole.MarkupLine($"  {item.SourceName}[[{item.SourceKey}]]: Last [yellow]{item.LastAction}[/] at {item.LastTimestamp:yyyy-MM-dd HH:mm:ss} by {item.LastUser}");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[green]Demo completed successfully![/]").RuleStyle("green"));
    }
}
