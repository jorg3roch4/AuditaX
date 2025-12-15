# Dapper + PostgreSQL + JSON

Complete guide for configuring AuditaX with Dapper, PostgreSQL, and JSON change log format.

## Required Packages

```
AuditaX
AuditaX.Dapper
AuditaX.PostgreSql
Dapper
Npgsql
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mydatabase;Username=postgres;Password=YourPassword"
  },
  "AuditaX": {
    "TableName": "audit_log",
    "Schema": "public",
    "ChangeLogFormat": "Json",
    "AutoCreateTable": true,
    "EnableLogging": true,
    "Entities": {
      "Product": {
        "SourceName": "Product",
        "KeyProperty": "ProductId",
        "AuditableProperties": [ "Name", "Description", "Price", "Stock" ]
      }
    }
  }
}
```

### Program.cs

```csharp
using AuditaX.Extensions;
using AuditaX.Dapper.Extensions;
using AuditaX.PostgreSql.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<DapperContext>();

builder.Services.AddAuditaX(builder.Configuration)
    .UseDapper<DapperContext>()
    .UsePostgreSql()
    .ValidateOnStartup();

builder.Services.AddScoped<IProductRepository, ProductRepository>();

var app = builder.Build();
app.Run();
```

### DapperContext

```csharp
using System.Data;
using Npgsql;

public class DapperContext(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")!;

    public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}
```

## Repository Implementation

```csharp
using Dapper;
using AuditaX.Dapper.Interfaces;

public class ProductRepository(DapperContext context, IAuditUnitOfWork audit) : IProductRepository
{
    public async Task<int> CreateAsync(Product product)
    {
        using var connection = context.CreateConnection();
        const string sql = @"
            INSERT INTO products (name, description, price, stock)
            VALUES (@Name, @Description, @Price, @Stock)
            RETURNING product_id";

        var id = await connection.QuerySingleAsync<int>(sql, product);
        product.ProductId = id;

        await audit.LogCreateAsync(product);

        return id;
    }

    public async Task<int> UpdateAsync(Product original, Product updated)
    {
        using var connection = context.CreateConnection();
        const string sql = @"
            UPDATE products
            SET name = @Name, description = @Description, price = @Price, stock = @Stock
            WHERE product_id = @ProductId";

        var affected = await connection.ExecuteAsync(sql, updated);

        if (affected > 0)
        {
            await audit.LogUpdateAsync(original, updated);
        }

        return affected;
    }

    public async Task<bool> DeleteAsync(Product product)
    {
        using var connection = context.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM products WHERE product_id = @ProductId",
            new { product.ProductId });

        if (affected > 0)
        {
            await audit.LogDeleteAsync(product);
        }

        return affected > 0;
    }
}
```

## Generated Audit Table

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

## Sample Audit Log Entry

```json
{
  "audit_log_id": 1,
  "source_name": "Product",
  "source_key": "42",
  "action": "Update",
  "changes": "{\"auditLog\":[{\"action\":\"Updated\",\"user\":\"admin@example.com\",\"timestamp\":\"2024-12-11T10:30:00Z\",\"fields\":[{\"name\":\"Price\",\"before\":\"9.99\",\"after\":\"12.99\"}]}]}",
  "user": "admin@example.com",
  "timestamp": "2024-12-11T10:30:00Z"
}
```

## Querying JSON Changes in PostgreSQL

```sql
-- Get all field changes from audit entries
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

-- Find all price changes
SELECT * FROM audit_log
WHERE changes::jsonb->'auditLog' @> '[{"fields": [{"name": "Price"}]}]';
```

## Running the Sample

```bash
cd samples/AuditaX.Sample.Dapper
dotnet run -- postgresql json
```
