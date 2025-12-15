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
WHERE changes IS NOT NULL;

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
cd samples/AuditaX.Sample.Dapper
dotnet run -- postgresql xml
```
