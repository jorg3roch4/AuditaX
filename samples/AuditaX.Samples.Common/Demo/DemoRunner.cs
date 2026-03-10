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

    public DemoRunner(IAuditService auditService)
    {
        _auditService = auditService;
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
            [new FieldChange { Name = "Tag", Value = tag1.Tag }]);

        AnsiConsole.MarkupLine($"Added Tag: [yellow]{tag1.Tag}[/] (ID: {tag1.Id})");

        var tag2 = new ProductTag { ProductId = product.Id, Tag = "RGB" };
        tag2 = await dataOps.CreateProductTagAsync(tag2);

        await _auditService.LogRelatedAsync(
            "Product",
            product.Id.ToString(),
            AuditAction.Added,
            "ProductTag",
            [new FieldChange { Name = "Tag", Value = tag2.Tag }]);

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
            [new FieldChange { Name = "Tag", Value = tag2.Tag }]);

        AnsiConsole.MarkupLine($"Removed Tag: [yellow]{tag2.Tag}[/]");
        AnsiConsole.MarkupLine("[dim]Related entity removal logged.[/]\n");

        // Demo 5: Soft Delete Product
        AnsiConsole.MarkupLine("[bold cyan]--- Demo 5: Soft Delete Product ---[/]");
        product.IsActive = false;
        await dataOps.UpdateProductAsync(product);
        await _auditService.LogDeleteAsync("Product", product.Id.ToString());

        AnsiConsole.MarkupLine($"Soft deleted Product ID: [yellow]{product.Id}[/]");
        AnsiConsole.MarkupLine("[dim]Delete logged to centralized AuditLog table.[/]\n");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[green]Demo completed successfully![/]").RuleStyle("green"));
    }
}
