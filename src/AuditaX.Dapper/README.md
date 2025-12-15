# AuditaX.Dapper

[![NuGet](https://img.shields.io/nuget/v/AuditaX.Dapper.svg?style=flat-square)](https://www.nuget.org/packages/AuditaX.Dapper)[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg?style=flat-square)](https://github.com/jorg3roch4/AuditaX/blob/main/LICENSE)

Dapper ORM support for [AuditaX](https://github.com/jorg3roch4/AuditaX) audit logging library.

## Installation

```bash
dotnet add package AuditaX
dotnet add package AuditaX.Dapper
dotnet add package AuditaX.SqlServer   # or AuditaX.PostgreSql
```

## Features

- `IAuditUnitOfWork` interface for repository-level audit logging
- Automatic change detection via reflection
- Works with any `DapperContext` that has a `CreateConnection()` method
- Manual control over when audit logs are created

## Configuration

### appsettings.json

```json
{
  "AuditaX": {
    "TableName": "AuditLog",
    "Schema": "dbo",
    "ChangeLogFormat": "Json",
    "AutoCreateTable": true,
    "EnableLogging": true,
    "Entities": {
      "Product": {
        "SourceName": "Product",
        "KeyProperty": "ProductId",
        "AuditableProperties": [ "Name", "Price", "Stock" ]
      }
    }
  }
}
```

### Service Registration

```csharp
services.AddAuditaX(configuration)
    .UseDapper<DapperContext>()
    .UseSqlServer()  // or .UsePostgreSql()
    .ValidateOnStartup();
```

## Usage

Inject `IAuditUnitOfWork` into your repositories:

```csharp
using AuditaX.Dapper.Interfaces;

public class ProductRepository(DapperContext context, IAuditUnitOfWork audit) : IProductRepository
{
    public async Task<int> CreateAsync(Product product)
    {
        using var connection = context.CreateConnection();
        // ... insert logic ...

        await audit.LogCreateAsync(product);
        return id;
    }

    public async Task<int> UpdateAsync(Product original, Product updated)
    {
        using var connection = context.CreateConnection();
        // ... update logic ...

        if (affected > 0)
        {
            await audit.LogUpdateAsync(original, updated);
        }
        return affected;
    }

    public async Task<bool> DeleteAsync(Product product)
    {
        using var connection = context.CreateConnection();
        // ... delete logic ...

        if (affected > 0)
        {
            await audit.LogDeleteAsync(product);
        }
        return affected > 0;
    }
}
```

## IAuditUnitOfWork Methods

| Method | Description |
|--------|-------------|
| `LogCreateAsync<T>(T entity)` | Logs entity creation |
| `LogUpdateAsync<T>(T original, T modified)` | Logs entity update with field changes |
| `LogDeleteAsync<T>(T entity)` | Logs entity deletion |

## How Change Detection Works

1. `LogUpdateAsync` receives original and modified entities
2. Uses reflection to compare configured `AuditableProperties`
3. Uses `IChangeLogService.HasChanged()` to detect differences
4. Only logs changes when values actually differ
5. Serializes changes to JSON or XML based on configuration

## DapperContext Requirements

Your `DapperContext` must have a public `CreateConnection()` method:

```csharp
public class DapperContext(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")!;

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
```

## Comparison with Entity Framework

| Feature | Dapper | Entity Framework |
|---------|--------|------------------|
| Change Tracking | Manual via `IAuditUnitOfWork` | Automatic via interceptors |
| Audit Timing | Explicit calls in repository | On `SaveChangesAsync()` |
| Control | Full control over when to audit | Automatic for all changes |

## Documentation

For complete documentation, see the [main AuditaX repository](https://github.com/jorg3roch4/AuditaX).

- [Dapper + SQL Server + JSON](https://github.com/jorg3roch4/AuditaX/blob/main/docs/dapper-sqlserver-json.md)
- [Dapper + SQL Server + XML](https://github.com/jorg3roch4/AuditaX/blob/main/docs/dapper-sqlserver-xml.md)
- [Dapper + PostgreSQL + JSON](https://github.com/jorg3roch4/AuditaX/blob/main/docs/dapper-postgresql-json.md)
- [Dapper + PostgreSQL + XML](https://github.com/jorg3roch4/AuditaX/blob/main/docs/dapper-postgresql-xml.md)
