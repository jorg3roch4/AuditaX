# AuditaX Documentation

Complete documentation for configuring and using AuditaX with different ORMs, databases, and serialization formats.

## Configuration Guides

Choose your combination:

### Dapper

| Database | Format | Guide |
|----------|--------|-------|
| SQL Server | JSON | [dapper-sqlserver-json.md](./dapper-sqlserver-json.md) |
| SQL Server | XML | [dapper-sqlserver-xml.md](./dapper-sqlserver-xml.md) |
| PostgreSQL | JSON | [dapper-postgresql-json.md](./dapper-postgresql-json.md) |
| PostgreSQL | XML | [dapper-postgresql-xml.md](./dapper-postgresql-xml.md) |

### Entity Framework Core

| Database | Format | Guide |
|----------|--------|-------|
| SQL Server | JSON | [efcore-sqlserver-json.md](./efcore-sqlserver-json.md) |
| SQL Server | XML | [efcore-sqlserver-xml.md](./efcore-sqlserver-xml.md) |
| PostgreSQL | JSON | [efcore-postgresql-json.md](./efcore-postgresql-json.md) |
| PostgreSQL | XML | [efcore-postgresql-xml.md](./efcore-postgresql-xml.md) |

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      Your Application                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────┐              ┌─────────────────┐       │
│  │   Repositories  │              │    DbContext    │       │
│  │  (with Dapper)  │              │   (with EF)     │       │
│  └────────┬────────┘              └────────┬────────┘       │
│           │                                │                │
│           ▼                                ▼                │
│  ┌─────────────────┐              ┌─────────────────┐       │
│  │ IAuditUnitOfWork│              │  Interceptors   │       │
│  │  (manual call)  │              │   (automatic)   │       │
│  └────────┬────────┘              └────────┬────────┘       │
│           │                                │                │
│           └────────────┬───────────────────┘                │
│                        ▼                                    │
│               ┌─────────────────┐                           │
│               │   IAuditService │                           │
│               └────────┬────────┘                           │
│                        │                                    │
│                        ▼                                    │
│               ┌─────────────────┐                           │
│               │ IAuditRepository│                           │
│               └────────┬────────┘                           │
│                        │                                    │
├────────────────────────┼────────────────────────────────────┤
│                        ▼                                    │
│               ┌─────────────────┐                           │
│               │IDatabaseProvider│                           │
│               │ (SqlServer/PG)  │                           │
│               └────────┬────────┘                           │
│                        │                                    │
│                        ▼                                    │
│               ┌─────────────────┐                           │
│               │    Database     │                           │
│               │   (AuditLog)    │                           │
│               └─────────────────┘                           │
└─────────────────────────────────────────────────────────────┘
```

## Package Dependencies

```
AuditaX (Core)
    │
    ├── AuditaX.Dapper
    │       └── Dapper
    │
    ├── AuditaX.EntityFramework
    │       └── Microsoft.EntityFrameworkCore.Relational
    │
    ├── AuditaX.SqlServer
    │       ├── Microsoft.Data.SqlClient
    │       └── Microsoft.EntityFrameworkCore.SqlServer
    │
    └── AuditaX.PostgreSql
            └── Npgsql.EntityFrameworkCore.PostgreSQL
```

## Key Interfaces

### IAuditService
Core service for logging audit events. Used internally by both Dapper and EF Core implementations.

### IAuditUnitOfWork (Dapper only)
Provides a simple API for repositories to log audit events:
- `LogCreateAsync<T>(T entity)`
- `LogUpdateAsync<T>(T original, T modified)`
- `LogDeleteAsync<T>(T entity)`

### IAuditRepository
Handles persistence of audit log entries to the database.

### IDatabaseProvider
Provides database-specific SQL queries and data type handling.

### IChangeLogService
Serializes field changes to JSON or XML format.

### IAuditUserProvider
Provides the current user for audit logging. Default implementation uses `HttpContext.User`.

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `TableName` | string | `"AuditLog"` | Name of the audit log table |
| `Schema` | string | `"dbo"` | Database schema |
| `LogFormat` | string | `"Json"` | `"Json"` or `"Xml"` |
| `AutoCreateTable` | bool | `true` | Create table if not exists |
| `EnableLogging` | bool | `true` | Enable/disable audit logging |
| `Entities` | object | - | Entity-specific configuration |

### Entity Configuration

```json
{
  "Entities": {
    "EntityTypeName": {
      "SourceName": "DisplayName",
      "Key": "PrimaryKeyPropertyName",
      "Properties": [ "Prop1", "Prop2", "Prop3" ]
    }
  }
}
```

## Fluent API Configuration

```csharp
services.AddAuditaX(options =>
{
    options.TableName = "AuditLog";
    options.Schema = "audit";
    options.LogFormat = LogFormat.Xml;
    options.AutoCreateTable = true;
    options.EnableLogging = true;

    options.ConfigureEntity<Product>("Product")
        .WithKey(p => p.ProductId)
        .Properties("Name", "Price", "Stock");
})
.UseDapper<DapperContext>()
.UseSqlServer()
.ValidateOnStartup();
```
