# Dapper + PostgreSQL + XML

Complete guide for configuring AuditaX with Dapper, PostgreSQL, and XML change log format.

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
    source_name VARCHAR(64)  NOT NULL,
    source_key  VARCHAR(64)  NOT NULL,
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

