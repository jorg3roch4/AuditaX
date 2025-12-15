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

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mydatabase;Username=postgres;Password=YourPassword"
  },
  "AuditaX": {
    "TableName": "audit_log",
    "Schema": "public",
    "ChangeLogFormat": "Xml",
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

## Sample Audit Log Entry (XML Format)

```json
{
  "audit_log_id": 1,
  "source_name": "Product",
  "source_key": "42",
  "action": "Update",
  "changes": "<AuditLog><Entry Action=\"Updated\" User=\"admin@example.com\" Timestamp=\"2024-12-11T10:30:00Z\"><Field Name=\"Price\" Before=\"9.99\" After=\"12.99\" /></Entry></AuditLog>",
  "user": "admin@example.com",
  "timestamp": "2024-12-11T10:30:00Z"
}
```

## Querying XML Changes in PostgreSQL

```sql
-- Get all field changes using xpath
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

-- Get specific field changes
SELECT
    audit_log_id,
    source_name,
    source_key,
    (xpath('/AuditLog/Entry/Field[@Name="Price"]/@Before', changes::xml))[1]::text AS old_price,
    (xpath('/AuditLog/Entry/Field[@Name="Price"]/@After', changes::xml))[1]::text AS new_price,
    "user",
    timestamp
FROM audit_log
WHERE action = 'Update';
```

## Running the Sample

```bash
cd samples/AuditaX.Sample.EntityFramework
dotnet run -- postgresql xml
```
