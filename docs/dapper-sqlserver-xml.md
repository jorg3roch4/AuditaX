# Dapper + SQL Server + XML

Complete guide for configuring AuditaX with Dapper, SQL Server, and XML change log format.

## Required Packages

```
AuditaX
AuditaX.Dapper
AuditaX.SqlServer
Dapper
Microsoft.Data.SqlClient
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
    options.TableName = "AuditLog";
    options.Schema = "dbo";
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
.UseDapper<DapperContext>()
.UseSqlServer()
.ValidateOnStartup();
```

### Program.cs

```csharp
using AuditaX.Extensions;
using AuditaX.Dapper.Extensions;
using AuditaX.SqlServer.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register DapperContext
builder.Services.AddScoped<DapperContext>();

// Option A: Configure from appsettings.json
builder.Services.AddAuditaX(builder.Configuration)
    .UseDapper<DapperContext>()
    .UseSqlServer()
    .ValidateOnStartup();

// Option B: Configure with Fluent API
// builder.Services.AddAuditaX(options => { /* see above */ })
//     .UseDapper<DapperContext>()
//     .UseSqlServer()
//     .ValidateOnStartup();

var app = builder.Build();
app.Run();
```

### DapperContext

```csharp
using System.Data;
using Microsoft.Data.SqlClient;

public class DapperContext(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")!;

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
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
            INSERT INTO Products (Name, Description, Price, Stock, IsActive)
            OUTPUT INSERTED.Id
            VALUES (@Name, @Description, @Price, @Stock, @IsActive)";

        product.Id = await connection.QuerySingleAsync<int>(sql, product);

        // Log the create action
        await auditService.LogCreateAsync("Product", product.Id.ToString());

        return product.Id;
    }

    public async Task UpdateAsync(Product original, Product updated)
    {
        using var connection = context.CreateConnection();
        const string sql = @"
            UPDATE Products
            SET Name = @Name, Description = @Description, Price = @Price, Stock = @Stock, IsActive = @IsActive
            WHERE Id = @Id";

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
        await connection.ExecuteAsync("DELETE FROM Products WHERE Id = @Id", new { product.Id });

        // Log the delete action
        await auditService.LogDeleteAsync("Product", product.Id.ToString());
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
