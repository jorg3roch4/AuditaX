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
using AuditaX.Dapper.Extensions;
using AuditaX.SqlServer.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<DapperContext>();

builder.Services.AddAuditaX(builder.Configuration)
    .UseDapper<DapperContext>()
    .UseSqlServer()
    .ValidateOnStartup();

builder.Services.AddScoped<IProductRepository, ProductRepository>();

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
            INSERT INTO Products (Name, Description, Price, Stock)
            OUTPUT INSERTED.ProductId
            VALUES (@Name, @Description, @Price, @Stock)";

        var id = await connection.QuerySingleAsync<int>(sql, product);
        product.ProductId = id;

        await audit.LogCreateAsync(product);

        return id;
    }

    public async Task<int> UpdateAsync(Product original, Product updated)
    {
        using var connection = context.CreateConnection();
        const string sql = @"
            UPDATE Products
            SET Name = @Name, Description = @Description, Price = @Price, Stock = @Stock
            WHERE ProductId = @ProductId";

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
            "DELETE FROM Products WHERE ProductId = @ProductId",
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
cd samples/AuditaX.Sample.Dapper
dotnet run -- sqlserver xml
```
