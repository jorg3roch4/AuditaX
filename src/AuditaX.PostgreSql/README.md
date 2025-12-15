# AuditaX.PostgreSql

[![NuGet](https://img.shields.io/nuget/v/AuditaX.PostgreSql.svg?style=flat-square)](https://www.nuget.org/packages/AuditaX.PostgreSql)[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg?style=flat-square)](https://github.com/jorg3roch4/AuditaX/blob/main/LICENSE)

PostgreSQL database provider for [AuditaX](https://github.com/jorg3roch4/AuditaX) audit logging library.

## Installation

```bash
dotnet add package AuditaX
dotnet add package AuditaX.PostgreSql
dotnet add package AuditaX.Dapper             # OR AuditaX.EntityFramework
```

## Features

- PostgreSQL-specific SQL query generation
- Support for `TEXT` type for change log storage
- `TIMESTAMPTZ` for timezone-aware timestamps
- `SERIAL` columns for auto-increment
- Snake_case naming convention support
- Compatible with PostgreSQL 12+

## Configuration

### Service Registration

```csharp
services.AddAuditaX(configuration)
    .UseDapper<DapperContext>()       // OR .UseEntityFramework<AppDbContext>()
    .UsePostgreSql()                  // Add PostgreSQL provider
    .ValidateOnStartup();
```

### Connection String

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mydatabase;Username=postgres;Password=YourPassword"
  }
}
```

### Recommended Configuration for PostgreSQL

```json
{
  "AuditaX": {
    "TableName": "audit_log",
    "Schema": "public",
    "ChangeLogFormat": "Json",
    "AutoCreateTable": true,
    "EnableLogging": true
  }
}
```

## Generated Table Schema

```sql
CREATE TABLE IF NOT EXISTS public.audit_log (
    audit_log_id SERIAL PRIMARY KEY,
    source_name VARCHAR(128) NOT NULL,
    source_key VARCHAR(128) NOT NULL,
    action VARCHAR(16) NOT NULL,
    changes TEXT,
    "user" VARCHAR(128),
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_audit_log_source ON public.audit_log (source_name, source_key);
CREATE INDEX IF NOT EXISTS ix_audit_log_timestamp ON public.audit_log (timestamp);
```

## Querying Audit Logs

### Basic Query

```sql
SELECT * FROM audit_log
WHERE source_name = 'Product'
ORDER BY timestamp DESC;
```

### Query JSON Changes

```sql
SELECT
    audit_log_id,
    source_name,
    source_key,
    entry->>'action' AS entry_action,
    entry->>'user' AS entry_user,
    field->>'name' AS field_name,
    field->>'before' AS old_value,
    field->>'after' AS new_value,
    "user",
    timestamp
FROM audit_log,
     jsonb_array_elements(changes::jsonb->'auditLog') AS entry,
     jsonb_array_elements(entry->'fields') AS field
WHERE action = 'Update';
```

### Filter by Specific Field Change

```sql
-- Find all price changes
SELECT * FROM audit_log
WHERE changes::jsonb->'auditLog' @> '[{"fields": [{"name": "Price"}]}]';
```

### Query XML Changes

```sql
SELECT
    audit_log_id,
    source_name,
    source_key,
    unnest(xpath('//Entry/@Action', changes::xml))::text AS entry_action,
    unnest(xpath('//Entry/@User', changes::xml))::text AS entry_user,
    unnest(xpath('//Field/@Name', changes::xml))::text AS field_name,
    unnest(xpath('//Field/@Before', changes::xml))::text AS old_value,
    unnest(xpath('//Field/@After', changes::xml))::text AS new_value,
    "user",
    timestamp
FROM audit_log
WHERE action = 'Update';
```

## DapperContext for PostgreSQL

```csharp
using System.Data;
using Npgsql;

public class DapperContext(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")!;

    public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}
```

## Requirements

- PostgreSQL 12 or later
- `Npgsql` package (included as dependency)

## Documentation

For complete documentation, see the [main AuditaX repository](https://github.com/jorg3roch4/AuditaX).

- [Dapper + PostgreSQL + JSON](https://github.com/jorg3roch4/AuditaX/blob/main/docs/dapper-postgresql-json.md)
- [Dapper + PostgreSQL + XML](https://github.com/jorg3roch4/AuditaX/blob/main/docs/dapper-postgresql-xml.md)
- [EF Core + PostgreSQL + JSON](https://github.com/jorg3roch4/AuditaX/blob/main/docs/efcore-postgresql-json.md)
- [EF Core + PostgreSQL + XML](https://github.com/jorg3roch4/AuditaX/blob/main/docs/efcore-postgresql-xml.md)
