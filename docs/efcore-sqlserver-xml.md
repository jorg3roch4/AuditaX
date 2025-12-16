# Entity Framework Core + SQL Server + XML

Complete guide for configuring AuditaX with Entity Framework Core, SQL Server, and XML change log format.

## Required Packages

```
AuditaX
AuditaX.EntityFramework
AuditaX.SqlServer
Microsoft.EntityFrameworkCore.SqlServer
```

## Configuration

### Option A: appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyDatabase;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
  },
  "AuditaX": {
    "TableName": "AuditLog",
    "Schema": "dbo",
    "AutoCreateTable": true,
    "EnableLogging": true,
    "ChangeLogFormat": "Xml",
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
    options.TableName = "AuditLog";
    options.Schema = "dbo";
    options.AutoCreateTable = true;
    options.EnableLogging = true;
    options.ChangeLogFormat = ChangeLogFormat.Xml;

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
.UseEntityFramework<AppDbContext>()
.UseSqlServer()
.ValidateOnStartup();
```

### Program.cs

```csharp
using AuditaX.Extensions;
using AuditaX.EntityFramework.Extensions;
using AuditaX.SqlServer.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Option A: Configure from appsettings.json
builder.Services.AddAuditaX(builder.Configuration)
    .UseEntityFramework<AppDbContext>()
    .UseSqlServer()
    .ValidateOnStartup();

// Option B: Configure with Fluent API
// builder.Services.AddAuditaX(options => { /* see above */ })
//     .UseEntityFramework<AppDbContext>()
//     .UseSqlServer()
//     .ValidateOnStartup();

var app = builder.Build();
app.Run();
```

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
CREATE TABLE [dbo].[AuditLog] (
    [LogId]      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [SourceName] NVARCHAR(50)     NOT NULL,
    [SourceKey]  NVARCHAR(900)    NOT NULL,
    [AuditLog]   XML              NOT NULL,
    CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([LogId]),
    CONSTRAINT [UQ_AuditLog_Source] UNIQUE ([SourceName], [SourceKey])
);

CREATE INDEX [IX_AuditLog_SourceName] ON [dbo].[AuditLog] ([SourceName]) INCLUDE ([SourceKey]);
CREATE INDEX [IX_AuditLog_SourceKey] ON [dbo].[AuditLog] ([SourceKey]) INCLUDE ([SourceName]);
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
    <Field Name="Tag" After="Gaming" />
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
