# Dapper + SQL Server + JSON

Complete guide for configuring AuditaX with Dapper, SQL Server, and JSON change log format.

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
    "ChangeLogFormat": "Json",
    "AutoCreateTable": true,
    "EnableLogging": true,
    "Entities": {
      "Product": {
        "SourceName": "Product",
        "KeyProperty": "ProductId",
        "AuditableProperties": [ "Name", "Description", "Price", "Stock" ]
      },
      "Customer": {
        "SourceName": "Customer",
        "KeyProperty": "CustomerId",
        "AuditableProperties": [ "Name", "Email", "Phone" ]
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

// Register your DapperContext
builder.Services.AddScoped<DapperContext>();

// Configure AuditaX
builder.Services.AddAuditaX(builder.Configuration)
    .UseDapper<DapperContext>()
    .UseSqlServer()
    .ValidateOnStartup();

// Register repositories
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
    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<Product>("SELECT * FROM Products");
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        using var connection = context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Product>(
            "SELECT * FROM Products WHERE ProductId = @ProductId",
            new { ProductId = id });
    }

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
        const string sql = "DELETE FROM Products WHERE ProductId = @ProductId";

        var affected = await connection.ExecuteAsync(sql, new { product.ProductId });

        if (affected > 0)
        {
            await audit.LogDeleteAsync(product);
        }

        return affected > 0;
    }
}
```

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

CREATE INDEX IX_AuditLog_SourceName_SourceKey ON [dbo].[AuditLog] ([SourceName], [SourceKey]);
CREATE INDEX IX_AuditLog_Timestamp ON [dbo].[AuditLog] ([Timestamp]);
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
cd samples/AuditaX.Sample.Dapper
dotnet run -- sqlserver json
```
