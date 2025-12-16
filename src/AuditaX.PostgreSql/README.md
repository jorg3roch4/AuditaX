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
- Support for native `JSONB` type for JSON storage (efficient indexing and queries)
- Native `XML` type for XML storage
- `TIMESTAMPTZ` for timezone-aware timestamps
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
    "LogFormat": "Json",
    "AutoCreateTable": true,
    "EnableLogging": true
  }
}
```

## Generated Table Schema

```sql
-- For JSON format (uses native JSONB type)
CREATE TABLE IF NOT EXISTS public.audit_log (
    log_id UUID NOT NULL DEFAULT gen_random_uuid(),
    source_name VARCHAR(50) NOT NULL,
    source_key VARCHAR(900) NOT NULL,
    audit_log JSONB NOT NULL,
    CONSTRAINT pk_audit_log PRIMARY KEY (log_id),
    CONSTRAINT uq_audit_log_source UNIQUE (source_name, source_key)
);

-- For XML format (uses native XML type)
CREATE TABLE IF NOT EXISTS public.audit_log (
    log_id UUID NOT NULL DEFAULT gen_random_uuid(),
    source_name VARCHAR(50) NOT NULL,
    source_key VARCHAR(900) NOT NULL,
    audit_log XML NOT NULL,
    CONSTRAINT pk_audit_log PRIMARY KEY (log_id),
    CONSTRAINT uq_audit_log_source UNIQUE (source_name, source_key)
);

CREATE INDEX IF NOT EXISTS ix_audit_log_source_name ON public.audit_log (source_name);
CREATE INDEX IF NOT EXISTS ix_audit_log_source_key ON public.audit_log (source_key);
```

## Querying Audit Logs

### Basic Query

```sql
SELECT * FROM audit_log
WHERE source_name = 'Product';
```

### Query JSON Changes

```sql
SELECT
    log_id,
    source_name,
    source_key,
    entry->>'action' AS entry_action,
    entry->>'user' AS entry_user,
    entry->>'timestamp' AS entry_timestamp,
    field->>'name' AS field_name,
    field->>'before' AS old_value,
    field->>'after' AS new_value
FROM audit_log,
     jsonb_array_elements(audit_log->'auditLog') AS entry,
     jsonb_array_elements(entry->'fields') AS field
WHERE entry->>'action' = 'Updated';
```

### Filter by Specific Field Change

```sql
-- Find all price changes (uses JSONB containment operator for efficient indexing)
SELECT * FROM audit_log
WHERE audit_log->'auditLog' @> '[{"fields": [{"name": "Price"}]}]';
```

### Query XML Changes

```sql
SELECT
    log_id,
    source_name,
    source_key,
    unnest(xpath('//Entry/@Action', audit_log::xml))::text AS entry_action,
    unnest(xpath('//Entry/@User', audit_log::xml))::text AS entry_user,
    unnest(xpath('//Entry/@Timestamp', audit_log::xml))::text AS entry_timestamp,
    unnest(xpath('//Field/@Name', audit_log::xml))::text AS field_name,
    unnest(xpath('//Field/@Before', audit_log::xml))::text AS old_value,
    unnest(xpath('//Field/@After', audit_log::xml))::text AS new_value
FROM audit_log;
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
