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

### Option A: appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=MyDatabase;Username=postgres;Password=YourPassword;"
  },
  "AuditaX": {
    "TableName": "audit_log",
    "Schema": "public",
    "AutoCreateTable": true,
    "EnableLogging": true,
    "ChangeLogFormat": "Json",
    "Entities": {
      "Product": {
        "Key": "Id",
        "AuditProperties": ["Name", "Description", "Price", "Stock", "IsActive"],
        "RelatedEntities": {
          "ProductTag": {
            "ParentKey": "ProductId",
            "CaptureProperties": ["Tag"]
          }
        }
      }
    }
  }
}
```

### Option B: Fluent API

```csharp
builder.Services.AddAuditaX(options =>
{
    options.TableName = "audit_log";
    options.Schema = "public";
    options.AutoCreateTable = true;
    options.EnableLogging = true;
    options.ChangeLogFormat = ChangeLogFormat.Json;

    options.ConfigureEntities(entities =>
    {
        entities.AuditEntity<Product>("Product")
            .WithKey(p => p.Id)
            .AuditProperties("Name", "Description", "Price", "Stock", "IsActive")
            .WithRelatedEntity<ProductTag>("ProductTag")
                .WithParentKey(t => t.ProductId)
                .OnAdded(t => new Dictionary<string, string?> { ["Tag"] = t.Tag })
                .OnRemoved(t => new Dictionary<string, string?> { ["Tag"] = t.Tag });
    });
})
.UseDapper<DapperContext>()
.UsePostgreSql()
.ValidateOnStartup();
```

### Program.cs

```csharp
using AuditaX.Extensions;
using AuditaX.Dapper.Extensions;
using AuditaX.PostgreSql.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register DapperContext
builder.Services.AddScoped<DapperContext>();

// Option A: Configure from appsettings.json
builder.Services.AddAuditaX(builder.Configuration)
    .UseDapper<DapperContext>()
    .UsePostgreSql()
    .ValidateOnStartup();

// Option B: Configure with Fluent API
// builder.Services.AddAuditaX(options => { /* see above */ })
//     .UseDapper<DapperContext>()
//     .UsePostgreSql()
//     .ValidateOnStartup();

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

## Usage

With Dapper, you need to manually call the audit methods after your data operations:

```csharp
using Dapper;
using AuditaX.Interfaces;

public class ProductRepository(DapperContext context, IAuditService auditService)
{
    public async Task<int> CreateAsync(Product product)
    {
        using var connection = context.CreateConnection();
        const string sql = @"
            INSERT INTO products (name, description, price, stock, is_active)
            VALUES (@Name, @Description, @Price, @Stock, @IsActive)
            RETURNING id";

        product.Id = await connection.QuerySingleAsync<int>(sql, product);

        // Log the create action
        await auditService.LogCreateAsync("Product", product.Id.ToString());

        return product.Id;
    }

    public async Task UpdateAsync(Product original, Product updated)
    {
        using var connection = context.CreateConnection();
        const string sql = @"
            UPDATE products
            SET name = @Name, description = @Description, price = @Price, stock = @Stock, is_active = @IsActive
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, updated);

        // Log the update with field changes
        var changes = new List<FieldChange>();
        if (original.Price != updated.Price)
            changes.Add(new FieldChange { Name = "Price", Before = original.Price.ToString(), After = updated.Price.ToString() });
        if (original.Stock != updated.Stock)
            changes.Add(new FieldChange { Name = "Stock", Before = original.Stock.ToString(), After = updated.Stock.ToString() });

        await auditService.LogUpdateAsync("Product", updated.Id.ToString(), changes);
    }

    public async Task DeleteAsync(Product product)
    {
        using var connection = context.CreateConnection();
        await connection.ExecuteAsync("DELETE FROM products WHERE id = @Id", new { product.Id });

        // Log the delete action
        await auditService.LogDeleteAsync("Product", product.Id.ToString());
    }
}
```

## Generated Audit Table

```sql
CREATE TABLE public.audit_log (
    log_id      UUID         NOT NULL DEFAULT gen_random_uuid(),
    source_name VARCHAR(50)  NOT NULL,
    source_key  VARCHAR(900) NOT NULL,
    audit_log   JSONB        NOT NULL,
    CONSTRAINT pk_audit_log PRIMARY KEY (log_id),
    CONSTRAINT uq_audit_log_source UNIQUE (source_name, source_key)
);

CREATE INDEX ix_audit_log_source_name ON public.audit_log (source_name) INCLUDE (source_key);
CREATE INDEX ix_audit_log_source_key ON public.audit_log (source_key) INCLUDE (source_name);
```

## Sample Audit Log Entry

```json
{
  "auditLog": [
    {
      "action": "Created",
      "user": "admin@example.com",
      "timestamp": "2024-12-15T10:00:00Z",
      "fields": []
    },
    {
      "action": "Updated",
      "user": "admin@example.com",
      "timestamp": "2024-12-15T10:30:00Z",
      "fields": [
        { "name": "Price", "before": "79.99", "after": "69.99" },
        { "name": "Stock", "before": "100", "after": "95" }
      ]
    },
    {
      "action": "Added",
      "user": "admin@example.com",
      "timestamp": "2024-12-15T11:00:00Z",
      "related": "ProductTag",
      "fields": [
        { "name": "Tag", "after": "Gaming" }
      ]
    },
    {
      "action": "Deleted",
      "user": "admin@example.com",
      "timestamp": "2024-12-15T12:00:00Z",
      "fields": []
    }
  ]
}
```

## Querying Audit Logs

Inject `IAuditQueryService` to query audit logs:

```csharp
public class AuditController(IAuditQueryService auditQueryService)
{
    // ...
}
```

### Get audit log for a specific entity

```csharp
var result = await auditQueryService.GetBySourceNameAndKeyAsync("Product", "42");
```

**Returns:** `AuditQueryResult?`
| Property | Value |
|----------|-------|
| SourceName | Product |
| SourceKey | 42 |
| AuditLog | `{"auditLog":[...]}` |

### Get audit logs by entity type (with pagination)

```csharp
var results = await auditQueryService.GetBySourceNameAsync("Product", skip: 0, take: 100);
```

**Returns:** `IEnumerable<AuditQueryResult>`
| SourceName | SourceKey | AuditLog |
|------------|-----------|----------|
| Product | 1 | `{"auditLog":[...]}` |
| Product | 2 | `{"auditLog":[...]}` |
| Product | 42 | `{"auditLog":[...]}` |

### Get audit logs by action type

```csharp
var results = await auditQueryService.GetBySourceNameAndActionAsync("Product", AuditAction.Updated);
```

**Returns:** `IEnumerable<AuditQueryResult>`
| SourceName | SourceKey | AuditLog |
|------------|-----------|----------|
| Product | 42 | `{"auditLog":[{"action":"Updated",...}]}` |

### Get audit logs by date range

```csharp
var results = await auditQueryService.GetBySourceNameAndDateAsync(
    "Product",
    fromDate: DateTime.UtcNow.AddDays(-7),
    toDate: DateTime.UtcNow);
```

**Returns:** `IEnumerable<AuditQueryResult>`
| SourceName | SourceKey | AuditLog |
|------------|-----------|----------|
| Product | 42 | `{"auditLog":[...]}` |

### Get audit logs by action and date range

```csharp
var results = await auditQueryService.GetBySourceNameActionAndDateAsync(
    "Product",
    AuditAction.Updated,
    fromDate: DateTime.UtcNow.AddDays(-7),
    toDate: DateTime.UtcNow);
```

**Returns:** `IEnumerable<AuditQueryResult>`
| SourceName | SourceKey | AuditLog |
|------------|-----------|----------|
| Product | 42 | `{"auditLog":[{"action":"Updated",...}]}` |

### Get audit summary (last action per entity)

```csharp
var summaries = await auditQueryService.GetSummaryBySourceNameAsync("Product", skip: 0, take: 100);
```

**Returns:** `IEnumerable<AuditSummaryResult>`
| SourceName | SourceKey | LastAction | LastTimestamp | LastUser |
|------------|-----------|------------|---------------|----------|
| Product | 1 | Created | 2024-12-15T09:00:00Z | admin@example.com |
| Product | 2 | Updated | 2024-12-15T10:30:00Z | admin@example.com |
| Product | 42 | Deleted | 2024-12-15T12:00:00Z | admin@example.com |
