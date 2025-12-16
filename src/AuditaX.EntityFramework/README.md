# AuditaX.EntityFramework

[![NuGet](https://img.shields.io/nuget/v/AuditaX.EntityFramework.svg?style=flat-square)](https://www.nuget.org/packages/AuditaX.EntityFramework)[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg?style=flat-square)](https://github.com/jorg3roch4/AuditaX/blob/main/LICENSE)

Entity Framework Core support for [AuditaX](https://github.com/jorg3roch4/AuditaX) audit logging library.

## Installation

```bash
dotnet add package AuditaX
dotnet add package AuditaX.EntityFramework
dotnet add package AuditaX.SqlServer   # or AuditaX.PostgreSql
```

## Features

- Automatic audit logging via EF Core interceptors
- Captures Added, Modified, and Deleted entity states
- Automatic change detection using EF Core's change tracker
- Transaction-safe: audit logs saved in same transaction as entity changes
- Zero code changes in your services/repositories

## Configuration

### appsettings.json

```json
{
  "AuditaX": {
    "TableName": "AuditLog",
    "Schema": "dbo",
    "LogFormat": "Json",
    "AutoCreateTable": true,
    "EnableLogging": true,
    "Entities": {
      "Product": {
        "SourceName": "Product",
        "Key": "ProductId",
        "Properties": [ "Name", "Price", "Stock" ]
      }
    }
  }
}
```

### Service Registration

```csharp
// Register DbContext WITH AuditaX interceptors
services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
    options.AddAuditaXInterceptor(sp);  // Important!
});

// Configure AuditaX
services.AddAuditaX(configuration)
    .UseEntityFramework<AppDbContext>()
    .UseSqlServer()  // or .UsePostgreSql()
    .ValidateOnStartup();
```

## Usage

With Entity Framework Core, auditing is **automatic**. No changes needed in your code:

```csharp
public class ProductService(AppDbContext context)
{
    public async Task<Product> CreateAsync(Product product)
    {
        context.Products.Add(product);
        await context.SaveChangesAsync();  // Audit log created automatically!
        return product;
    }

    public async Task UpdateAsync(int id, string name, decimal price)
    {
        var product = await context.Products.FindAsync(id);
        product!.Name = name;
        product.Price = price;
        await context.SaveChangesAsync();  // Audit log created automatically!
    }

    public async Task DeleteAsync(int id)
    {
        var product = await context.Products.FindAsync(id);
        context.Products.Remove(product!);
        await context.SaveChangesAsync();  // Audit log created automatically!
    }
}
```

## How It Works

1. **Interceptor Registration**: `AddAuditaXInterceptor()` registers a `SaveChangesInterceptor`
2. **Change Detection**: When `SaveChangesAsync()` is called, the interceptor:
   - Enumerates all tracked entities
   - Filters for Added, Modified, and Deleted states
   - Checks if entity type is configured for auditing
3. **Change Comparison**: For Modified entities:
   - Gets original values from `OriginalValues`
   - Compares with current values
   - Creates `FieldChange` records for differences
4. **Audit Logging**: Creates audit log entries via `IAuditService`
5. **Transaction Safety**: All operations occur within the same transaction

## DbContext Requirements

Your DbContext must be registered with the AuditaX interceptors:

```csharp
services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseYourDatabase(connectionString);
    options.AddAuditaXInterceptor(sp);  // Required for automatic auditing
});
```

## Comparison with Dapper

| Feature | Entity Framework | Dapper |
|---------|------------------|--------|
| Change Tracking | Automatic via interceptors | Manual via `IAuditUnitOfWork` |
| Audit Timing | On `SaveChangesAsync()` | Explicit calls in repository |
| Control | Automatic for all changes | Full control over when to audit |
| Setup | Add interceptors to DbContext | Inject `IAuditUnitOfWork` |

## Documentation

For complete documentation, see the [main AuditaX repository](https://github.com/jorg3roch4/AuditaX).

- [EF Core + SQL Server + JSON](https://github.com/jorg3roch4/AuditaX/blob/main/docs/efcore-sqlserver-json.md)
- [EF Core + SQL Server + XML](https://github.com/jorg3roch4/AuditaX/blob/main/docs/efcore-sqlserver-xml.md)
- [EF Core + PostgreSQL + JSON](https://github.com/jorg3roch4/AuditaX/blob/main/docs/efcore-postgresql-json.md)
- [EF Core + PostgreSQL + XML](https://github.com/jorg3roch4/AuditaX/blob/main/docs/efcore-postgresql-xml.md)
