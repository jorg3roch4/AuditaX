# Entity Framework Core + PostgreSQL + JSON

Complete guide for configuring AuditaX with Entity Framework Core, PostgreSQL, and JSON change log format.

## Required Packages

```
AuditaX
AuditaX.EntityFramework
AuditaX.PostgreSql
Npgsql.EntityFrameworkCore.PostgreSQL
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
using AuditaX.EntityFramework.Extensions;
using AuditaX.PostgreSql.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.AddAuditaXInterceptors(sp);
});

builder.Services.AddAuditaX(builder.Configuration)
    .UseEntityFramework<AppDbContext>()
    .UsePostgreSql()
    .ValidateOnStartup();

var app = builder.Build();
app.Run();
```

### DbContext

```csharp
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.ProductId);
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Price).HasColumnName("price");
        });
    }
}
```

## Usage (Automatic Auditing)

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
        product!.Name = name;
        product.Price = price;
        await context.SaveChangesAsync(); // Audit log created automatically
    }

    public async Task DeleteAsync(int id)
    {
        var product = await context.Products.FindAsync(id);
        context.Products.Remove(product!);
        await context.SaveChangesAsync(); // Audit log created automatically
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
cd samples/AuditaX.Sample.EntityFramework
dotnet run -- postgresql json
```
