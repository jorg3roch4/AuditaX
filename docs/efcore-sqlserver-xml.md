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

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=localhost;Initial Catalog=MyDatabase;User ID=sa;Password=YourPassword;TrustServerCertificate=True"
  },
  "AuditaX": {
    "TableName": "AuditLog",
    "Schema": "dbo",
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
using AuditaX.SqlServer.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.AddAuditaXInterceptors(sp);
});

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
  "AuditLogId": 1,
  "SourceName": "Product",
  "SourceKey": "42",
  "Action": "Update",
  "Changes": "<AuditLog><Entry Action=\"Updated\" User=\"admin@example.com\" Timestamp=\"2024-12-11T10:30:00Z\"><Field Name=\"Price\" Before=\"9.99\" After=\"12.99\" /><Field Name=\"Stock\" Before=\"100\" After=\"85\" /></Entry></AuditLog>",
  "User": "admin@example.com",
  "Timestamp": "2024-12-11T10:30:00Z"
}
```

## Querying XML Changes in SQL Server

```sql
-- Get all field changes from audit entries
SELECT
    AuditLogId,
    SourceName,
    SourceKey,
    e.value('@Action', 'NVARCHAR(16)') AS EntryAction,
    e.value('@User', 'NVARCHAR(128)') AS EntryUser,
    f.value('@Name', 'NVARCHAR(128)') AS FieldName,
    f.value('@Before', 'NVARCHAR(MAX)') AS OldValue,
    f.value('@After', 'NVARCHAR(MAX)') AS NewValue,
    [User],
    [Timestamp]
FROM AuditLog
CROSS APPLY Changes.nodes('/AuditLog/Entry') AS t(e)
CROSS APPLY e.nodes('Field') AS u(f)
WHERE Action = 'Update';

-- Get all price changes
SELECT
    AuditLogId,
    SourceName,
    SourceKey,
    f.value('@Name', 'NVARCHAR(128)') AS FieldName,
    f.value('@Before', 'NVARCHAR(MAX)') AS OldValue,
    f.value('@After', 'NVARCHAR(MAX)') AS NewValue,
    [User],
    [Timestamp]
FROM AuditLog
CROSS APPLY Changes.nodes('/AuditLog/Entry/Field') AS t(f)
WHERE f.value('@Name', 'NVARCHAR(128)') = 'Price';
```

## Running the Sample

```bash
cd samples/AuditaX.Sample.EntityFramework
dotnet run -- sqlserver xml
```
