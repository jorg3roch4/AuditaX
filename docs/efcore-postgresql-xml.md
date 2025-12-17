# Entity Framework Core + PostgreSQL + XML

Complete guide for configuring AuditaX with Entity Framework Core, PostgreSQL, and XML change log format.

## Required Packages

```
AuditaX
AuditaX.EntityFramework
AuditaX.PostgreSql
Npgsql.EntityFrameworkCore.PostgreSQL
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
    "LogFormat": "Xml",
    "Entities": {
      "Product": {
        "Key": "Id",
        "Properties": ["Name", "Description", "Price", "Stock", "IsActive"],
        "RelatedEntities": {
          "ProductTag": {
            "ParentKey": "ProductId",
            "Properties": ["Tag"]
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
    options.LogFormat = LogFormat.Xml;

    options.ConfigureEntity<Product>("Product")
        .WithKey(p => p.Id)
        .Properties("Name", "Description", "Price", "Stock", "IsActive")
        .WithRelatedEntity<ProductTag>("ProductTag")
            .WithParentKey(t => t.ProductId)
            .Properties("Tag");
})
.UseEntityFramework<AppDbContext>()
.UsePostgreSql()
.ValidateOnStartup();
```

### Program.cs

```csharp
using AuditaX.Extensions;
using AuditaX.EntityFramework.Extensions;
using AuditaX.PostgreSql.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Step 1: Configure AuditaX FIRST (before DbContext registration)
// Option A: Configure from appsettings.json
builder.Services.AddAuditaX(builder.Configuration)
    .UseEntityFramework<AppDbContext>()
    .UsePostgreSql()
    .ValidateOnStartup();

// Option B: Configure with Fluent API
// builder.Services.AddAuditaX(options => { /* see above */ })
//     .UseEntityFramework<AppDbContext>()
//     .UsePostgreSql()
//     .ValidateOnStartup();

// Step 2: Register DbContext WITH AuditaX
// IMPORTANT: Use (sp, options) to access the service provider
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));

    // This line enables automatic audit logging - REQUIRED for EF Core
    options.UseAuditaX(sp);
});

var app = builder.Build();
app.Run();
```

> **Important:** The call to `UseAuditaX(sp)` is required for automatic change tracking. Without it, entity changes will not be audited.

> **Warning:** Do NOT use `QueryTrackingBehavior.NoTracking` in your DbContext. AuditaX requires the ChangeTracker to detect entity changes.

### AppDbContext

```csharp
using Microsoft.EntityFrameworkCore;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductTag> ProductTags => Set<ProductTag>();
}
```

## Usage

With Entity Framework Core, auditing is **automatic**. The interceptor tracks all changes made through the DbContext:

```csharp
public class ProductService(AppDbContext context)
{
    public async Task<Product> CreateAsync(Product product)
    {
        context.Products.Add(product);
        await context.SaveChangesAsync(); // Audit log created automatically
        return product;
    }

    public async Task UpdateAsync(int id, string name, decimal price)
    {
        var product = await context.Products.FindAsync(id);
        if (product is null) throw new KeyNotFoundException();

        product.Name = name;
        product.Price = price;
        await context.SaveChangesAsync(); // Audit log created automatically
    }

    public async Task DeleteAsync(int id)
    {
        var product = await context.Products.FindAsync(id);
        if (product is null) throw new KeyNotFoundException();

        context.Products.Remove(product);
        await context.SaveChangesAsync(); // Audit log created automatically
    }
}
```

## Generated Audit Table

```sql
CREATE TABLE public.audit_log (
    log_id      UUID         NOT NULL DEFAULT gen_random_uuid(),
    source_name VARCHAR(50)  NOT NULL,
    source_key  VARCHAR(900) NOT NULL,
    audit_log   XML          NOT NULL,
    CONSTRAINT pk_audit_log PRIMARY KEY (log_id),
    CONSTRAINT uq_audit_log_source UNIQUE (source_name, source_key)
);

CREATE INDEX ix_audit_log_source_name ON public.audit_log (source_name) INCLUDE (source_key);
CREATE INDEX ix_audit_log_source_key ON public.audit_log (source_key) INCLUDE (source_name);
```

## Sample Audit Log Entry

```xml
<AuditLog>
  <Entry Action="Created" User="admin@example.com" Timestamp="2024-12-15T10:00:00Z" />
  <Entry Action="Updated" User="admin@example.com" Timestamp="2024-12-15T10:30:00Z">
    <Field Name="Price" Before="79.99" After="69.99" />
    <Field Name="Stock" Before="100" After="95" />
  </Entry>
  <Entry Action="Added" User="admin@example.com" Timestamp="2024-12-15T11:00:00Z" Related="ProductTag">
    <Field Name="Tag" Value="Gaming" />
  </Entry>
  <Entry Action="Removed" User="admin@example.com" Timestamp="2024-12-15T11:30:00Z" Related="ProductTag">
    <Field Name="Tag" Value="Gaming" />
  </Entry>
  <Entry Action="Deleted" User="admin@example.com" Timestamp="2024-12-15T12:00:00Z" />
</AuditLog>
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
| AuditLog | `<AuditLog>...</AuditLog>` |

### Get audit logs by entity type (with pagination)

```csharp
var results = await auditQueryService.GetBySourceNameAsync("Product", skip: 0, take: 100);
```

**Returns:** `IEnumerable<AuditQueryResult>`
| SourceName | SourceKey | AuditLog |
|------------|-----------|----------|
| Product | 1 | `<AuditLog>...</AuditLog>` |
| Product | 2 | `<AuditLog>...</AuditLog>` |
| Product | 42 | `<AuditLog>...</AuditLog>` |

### Get audit logs by action type

```csharp
var results = await auditQueryService.GetBySourceNameAndActionAsync("Product", AuditAction.Updated);
```

**Returns:** `IEnumerable<AuditQueryResult>`
| SourceName | SourceKey | AuditLog |
|------------|-----------|----------|
| Product | 42 | `<AuditLog><Entry Action="Updated"...>...</AuditLog>` |

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
| Product | 42 | `<AuditLog>...</AuditLog>` |

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
| Product | 42 | `<AuditLog><Entry Action="Updated"...>...</AuditLog>` |

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
