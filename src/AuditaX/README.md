# AuditaX

Core library for AuditaX audit logging system.

## Installation

This is the core package. You also need an ORM package and a database provider:

```
AuditaX                    # This package (required)
AuditaX.Dapper             # For Dapper ORM
AuditaX.EntityFramework    # For Entity Framework Core
AuditaX.SqlServer          # For SQL Server database
AuditaX.PostgreSql         # For PostgreSQL database
```

## Features

- Core interfaces and services for audit logging
- Configuration via `appsettings.json` or Fluent API
- JSON and XML change log serialization
- Entity configuration with auditable properties
- User provider integration via `HttpContext`
- Startup validation

## Core Interfaces

| Interface | Description |
|-----------|-------------|
| `IAuditService` | Main service for logging audit events |
| `IAuditRepository` | Persistence layer for audit log entries |
| `IChangeLogService` | Serializes field changes to JSON/XML |
| `IAuditUserProvider` | Provides current user for audit entries |
| `IAuditStartupValidator` | Validates configuration on startup |
| `IDatabaseProvider` | Database-specific SQL and types |

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

### Fluent API

```csharp
services.AddAuditaX(options =>
{
    options.TableName = "AuditLog";
    options.Schema = "audit";
    options.LogFormat = LogFormat.Json;
    options.AutoCreateTable = true;
    options.EnableLogging = true;

    options.ConfigureEntity<Product>("Product")
        .WithKey(p => p.ProductId)
        .Properties("Name", "Price", "Stock");
});
```

## Service Registration

```csharp
services.AddAuditaX(configuration)    // Load from appsettings.json
    .UseDapper<DapperContext>()       // OR .UseEntityFramework<AppDbContext>()
    .UseSqlServer()                   // OR .UsePostgreSql()
    .ValidateOnStartup();             // Optional: validate on startup
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `TableName` | string | `"AuditLog"` | Audit log table name |
| `Schema` | string | `"dbo"` | Database schema |
| `LogFormat` | enum | `Json` | `Json` or `Xml` |
| `AutoCreateTable` | bool | `true` | Create table if not exists |
| `EnableLogging` | bool | `true` | Enable/disable auditing |

## Entity Configuration

Each entity to be audited needs configuration:

```json
{
  "Entities": {
    "EntityTypeName": {
      "SourceName": "DisplayNameInAuditLog",
      "Key": "PrimaryKeyPropertyName",
      "Properties": [ "Prop1", "Prop2" ]
    }
  }
}
```

- **SourceName**: Name stored in audit log (defaults to type name)
- **Key**: Property used as `SourceKey` in audit log
- **Properties**: Properties tracked for changes (Update action)

## Custom User Provider

Default implementation uses `HttpContext.User`. To customize:

```csharp
public class CustomUserProvider : IAuditUserProvider
{
    public string GetCurrentUser()
    {
        // Your custom logic
        return "system";
    }
}

// Register
services.AddScoped<IAuditUserProvider, CustomUserProvider>();
```

## Documentation

See the [main README](../../README.md) for complete documentation.
