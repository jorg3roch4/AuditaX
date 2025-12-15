# Entity Framework Core + SQL Server + JSON

Complete guide for configuring AuditaX with Entity Framework Core, SQL Server, and JSON change log format.

## Required Packages

```
AuditaX
AuditaX.EntityFramework
AuditaX.SqlServer
Microsoft.EntityFrameworkCore.SqlServer
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=localhost;Initial Catalog=MyDatabase;User ID=sa;Password=YourPassword;TrustServerCertificate=True"
  },
  "AuditaX": {
    "TableName": "AuditLog",
    "Schema": "dbo",
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
using AuditaX.SqlServer.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register DbContext with AuditaX interceptors
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.AddAuditaXInterceptors(sp); // Add AuditaX interceptors
});

// Configure AuditaX
builder.Services.AddAuditaX(builder.Configuration)
    .UseEntityFramework<AppDbContext>()
    .UseSqlServer()
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
            entity.HasKey(e => e.ProductId);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });
    }
}
```

## Usage (Automatic Auditing)

With Entity Framework Core, auditing is **automatic**. The interceptors track all changes made through the `DbContext`:

```csharp
public class ProductService(AppDbContext context)
{
    public async Task<Product> CreateAsync(Product product)
    {
        context.Products.Add(product);
        await context.SaveChangesAsync(); // Audit log created automatically

        return product;
    }

    public async Task<Product> UpdateAsync(int id, string name, decimal price)
    {
        var product = await context.Products.FindAsync(id);
        if (product is null) throw new NotFoundException();

        product.Name = name;
        product.Price = price;

        await context.SaveChangesAsync(); // Audit log created automatically

        return product;
    }

    public async Task DeleteAsync(int id)
    {
        var product = await context.Products.FindAsync(id);
        if (product is null) throw new NotFoundException();

        context.Products.Remove(product);
        await context.SaveChangesAsync(); // Audit log created automatically
    }
}
```

## How It Works

1. AuditaX registers a `SaveChangesInterceptor` with your DbContext
2. When `SaveChangesAsync()` is called, the interceptor:
   - Captures all Added, Modified, and Deleted entities
   - For Modified entities, compares original vs current values
   - Creates audit log entries for configured entities
3. Audit entries are saved in the same transaction as your changes

## Generated Audit Table

```sql
CREATE TABLE [dbo].[AuditLog] (
    [AuditLogId] INT IDENTITY(1,1) PRIMARY KEY,
    [SourceName] NVARCHAR(128) NOT NULL,
    [SourceKey] NVARCHAR(128) NOT NULL,
    [Action] NVARCHAR(16) NOT NULL,
    [Changes] NVARCHAR(MAX) NULL,
    [User] NVARCHAR(128) NULL,
    [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

## Sample Audit Log Entry

```json
{
  "AuditLogId": 1,
  "SourceName": "Product",
  "SourceKey": "42",
  "Action": "Update",
  "Changes": "{\"auditLog\":[{\"action\":\"Updated\",\"user\":\"admin@example.com\",\"timestamp\":\"2024-12-11T10:30:00Z\",\"fields\":[{\"name\":\"Price\",\"before\":\"9.99\",\"after\":\"12.99\"},{\"name\":\"Stock\",\"before\":\"100\",\"after\":\"85\"}]}]}",
  "User": "admin@example.com",
  "Timestamp": "2024-12-11T10:30:00Z"
}
```

## Querying JSON Changes in SQL Server

```sql
-- Get all field changes from audit entries
SELECT
    AuditLogId,
    SourceName,
    SourceKey,
    JSON_VALUE(f.value, '$.name') AS FieldName,
    JSON_VALUE(f.value, '$.before') AS OldValue,
    JSON_VALUE(f.value, '$.after') AS NewValue,
    JSON_VALUE(e.value, '$.user') AS EntryUser,
    JSON_VALUE(e.value, '$.action') AS EntryAction,
    [User],
    [Timestamp]
FROM AuditLog
CROSS APPLY OPENJSON(Changes, '$.auditLog') AS e
CROSS APPLY OPENJSON(e.value, '$.fields') AS f
WHERE Action = 'Update';
```

## Running the Sample

```bash
cd samples/AuditaX.Sample.EntityFramework
dotnet run -- sqlserver json
```
