![AuditaX Logo](https://raw.githubusercontent.com/jorg3roch4/AuditaX/main/assets/auditax-brand.png)

# AuditaX

**Flexible Entity Audit Logging for .NET 10+**

[![NuGet](https://img.shields.io/nuget/v/AuditaX.svg?style=flat-square)](https://www.nuget.org/packages/AuditaX)[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg?style=flat-square)](https://github.com/jorg3roch4/AuditaX/blob/main/LICENSE)[![C#](https://img.shields.io/badge/C%23-14-239120.svg?style=flat-square)](https://docs.microsoft.com/en-us/dotnet/csharp/)[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg?style=flat-square)](https://dotnet.microsoft.com/)

**AuditaX** is a modern, extensible audit logging library for .NET applications. It provides a unified approach to tracking entity changes across different ORMs and database providers, with support for both automatic and manual audit control.

Built exclusively for **.NET 10** with **C# 14**, AuditaX offers fluent configuration, multiple serialization formats, and seamless integration with your existing data access layer.

---

## 💖 Support the Project

AuditaX is a passion project, driven by the desire to provide a truly modern audit logging solution for the .NET community. Maintaining this library requires significant effort: staying current with each .NET release, addressing issues promptly, implementing new features, keeping documentation up to date, and ensuring compatibility across different ORMs and database providers.

If AuditaX has helped you build better applications or saved you development time, I would be incredibly grateful for your support. Your contribution—no matter the size—helps me dedicate time to respond to issues quickly, implement improvements, and keep the library evolving alongside the .NET platform.

**I'm also looking for sponsors** who believe in this project's mission. Sponsorship helps ensure AuditaX remains actively maintained and continues to serve the .NET community for years to come.

Of course, there's absolutely no obligation. If you prefer, simply starring the repository or sharing AuditaX with fellow developers is equally appreciated!

- ⭐ **Star the repository** on GitHub to raise its visibility
- 💬 **Share** AuditaX with your team or community
- ☕ **Support via Donations:**

  - [![PayPal](https://img.shields.io/badge/PayPal-Donate-00457C?style=for-the-badge&logo=paypal&logoColor=white)](https://paypal.me/jorg3roch4)
  - [![Ko-fi](https://img.shields.io/badge/Ko--fi-Support-FF5E5B?style=for-the-badge&logo=ko-fi&logoColor=white)](https://ko-fi.com/jorg3roch4)

---

## What's New in 1.0.0

**Initial Release!** AuditaX 1.0.0 provides a complete audit logging solution:

- Multiple ORM Support: Dapper and Entity Framework Core
- Multiple Database Providers: SQL Server and PostgreSQL
- Flexible Change Log Format: JSON or XML serialization
- Automatic Change Tracking: EF Core interceptors
- Manual Audit Control: `IAuditUnitOfWork` for Dapper
- Configuration Options: appsettings.json or Fluent API
- Auto Table Creation: Creates audit table on startup
- Startup Validation: Validates configuration and connectivity

---

## Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| `AuditaX` | Core library with interfaces and base services | [![NuGet](https://img.shields.io/nuget/v/AuditaX.svg?style=flat-square)](https://www.nuget.org/packages/AuditaX) |
| `AuditaX.Dapper` | Dapper ORM support with `IAuditUnitOfWork` | [![NuGet](https://img.shields.io/nuget/v/AuditaX.Dapper.svg?style=flat-square)](https://www.nuget.org/packages/AuditaX.Dapper) |
| `AuditaX.EntityFramework` | EF Core support with automatic interceptors | [![NuGet](https://img.shields.io/nuget/v/AuditaX.EntityFramework.svg?style=flat-square)](https://www.nuget.org/packages/AuditaX.EntityFramework) |
| `AuditaX.SqlServer` | SQL Server database provider | [![NuGet](https://img.shields.io/nuget/v/AuditaX.SqlServer.svg?style=flat-square)](https://www.nuget.org/packages/AuditaX.SqlServer) |
| `AuditaX.PostgreSql` | PostgreSQL database provider | [![NuGet](https://img.shields.io/nuget/v/AuditaX.PostgreSql.svg?style=flat-square)](https://www.nuget.org/packages/AuditaX.PostgreSql) |

---

## Getting Started

### Installation

Choose packages based on your ORM and database:

**For Dapper + SQL Server:**
```bash
dotnet add package AuditaX
dotnet add package AuditaX.Dapper
dotnet add package AuditaX.SqlServer
```

**For EF Core + PostgreSQL:**
```bash
dotnet add package AuditaX
dotnet add package AuditaX.EntityFramework
dotnet add package AuditaX.PostgreSql
```

### Configuration (appsettings.json)

```json
{
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
        "AuditableProperties": [ "Name", "Price", "Stock" ]
      }
    }
  }
}
```

### Service Registration

**Dapper + SQL Server:**
```csharp
services.AddAuditaX(configuration)
    .UseDapper<DapperContext>()
    .UseSqlServer()
    .ValidateOnStartup();
```

**EF Core + PostgreSQL:**
```csharp
services.AddAuditaX(configuration)
    .UseEntityFramework<AppDbContext>()
    .UsePostgreSql()
    .ValidateOnStartup();
```

---

## Features

### Core Capabilities
- **Fluent Configuration** - Configure via appsettings.json or fluent API
- **Entity Configuration** - Define which entities and properties to audit
- **Change Serialization** - JSON or XML format for change logs
- **Auto Table Creation** - Automatically creates the audit table on startup
- **Startup Validation** - Validates configuration and database connectivity

### ORM Integration
- **Dapper Support** - Manual audit control with `IAuditUnitOfWork`
- **EF Core Support** - Automatic change tracking via SaveChanges interceptors
- **User Provider** - Inject `IUserProvider` to capture the current user

### Database Providers
- **SQL Server** - Native support with optimized queries
- **PostgreSQL** - Native support with optimized queries

---

## Usage

### With Dapper (Manual Audit)

Inject `IAuditUnitOfWork` into your repositories:

```csharp
public class ProductRepository(DapperContext context, IAuditUnitOfWork audit)
{
    public async Task<int> CreateAsync(Product product)
    {
        using var connection = context.CreateConnection();
        const string sql = "INSERT INTO Products (...) OUTPUT INSERTED.ProductId VALUES (...)";

        var id = await connection.QuerySingleAsync<int>(sql, product);
        product.ProductId = id;

        await audit.LogCreateAsync(product);

        return id;
    }

    public async Task<int> UpdateAsync(Product original, Product updated)
    {
        using var connection = context.CreateConnection();
        const string sql = "UPDATE Products SET ... WHERE ProductId = @ProductId";

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

### With Entity Framework Core (Automatic)

EF Core uses interceptors for automatic audit logging. Just configure your entities and AuditaX handles the rest:

```csharp
// Entity changes are automatically tracked
var product = new Product { Name = "Widget", Price = 9.99m };
dbContext.Products.Add(product);
await dbContext.SaveChangesAsync(); // Audit log created automatically

product.Price = 12.99m;
await dbContext.SaveChangesAsync(); // Update audit log created automatically
```

---

## Audit Log Structure

| Column | Type | Description |
|--------|------|-------------|
| `AuditLogId` | INT/SERIAL | Primary key (auto-increment) |
| `SourceName` | VARCHAR(128) | Entity/table name |
| `SourceKey` | VARCHAR(128) | Primary key value |
| `Action` | VARCHAR(16) | Create, Update, or Delete |
| `Changes` | NVARCHAR(MAX)/TEXT | JSON or XML serialized changes |
| `User` | VARCHAR(128) | User who made the change |
| `Timestamp` | DATETIME/TIMESTAMPTZ | When the change occurred |

---

## Change Log Formats

### JSON Format
```json
{
  "auditLog": [
    {
      "action": "Created",
      "user": "demo@auditax.sample",
      "timestamp": "2025-12-11T12:55:38.4907999Z"
    },
    {
      "action": "Updated",
      "user": "demo@auditax.sample",
      "timestamp": "2025-12-11T12:55:38.6169691Z",
      "fields": [
        { "name": "Price", "before": "79.99", "after": "69.99" },
        { "name": "Stock", "before": "100", "after": "95" }
      ]
    },
    {
      "action": "Added",
      "user": "demo@auditax.sample",
      "timestamp": "2025-12-11T12:55:38.6777639Z",
      "related": "ProductTag",
      "fields": [
        { "name": "Tag", "after": "Gaming" }
      ]
    },
    {
      "action": "Deleted",
      "user": "demo@auditax.sample",
      "timestamp": "2025-12-11T12:55:38.7575026Z"
    }
  ]
}
```

### XML Format
```xml
<AuditLog>
  <Entry Action="Created" User="Anonymous" Timestamp="2025-12-12T14:38:01.9671416Z" />
  <Entry Action="Updated" User="Anonymous" Timestamp="2025-12-12T14:41:12.5715243Z">
    <Field Name="Price" Before="9.99" After="12.99" />
    <Field Name="Stock" Before="100" After="85" />
  </Entry>
</AuditLog>
```

---

## Querying Audit Logs

AuditaX provides `IAuditQueryService` for querying audit history. Inject it into your services:

```csharp
public class ProductService(IAuditQueryService auditQueryService)
{
    public async Task<AuditQueryResult?> GetProductHistoryAsync(int productId)
    {
        return await auditQueryService.GetBySourceNameAndKeyAsync("Product", productId.ToString());
    }

    public async Task<IEnumerable<AuditSummaryResult>> GetRecentChangesAsync()
    {
        return await auditQueryService.GetSummaryBySourceNameAsync("Product", skip: 0, take: 10);
    }
}
```

**Summary Result (one record per entity with last action):**
```json
[
  { "sourceName": "Product", "sourceKey": "1", "lastAction": "Updated", "lastTimestamp": "2025-12-15T14:30:00Z", "lastUser": "sales@example.com" },
  { "sourceName": "Product", "sourceKey": "2", "lastAction": "Created", "lastTimestamp": "2025-12-15T10:00:00Z", "lastUser": "admin@example.com" },
  { "sourceName": "Product", "sourceKey": "3", "lastAction": "Deleted", "lastTimestamp": "2025-12-14T17:45:00Z", "lastUser": "admin@example.com" }
]
```

### Available Query Methods

| Method | Description |
|--------|-------------|
| `GetBySourceNameAsync` | Get all audit logs for an entity type (paginated) |
| `GetBySourceNameAndKeyAsync` | Get audit log for a specific entity instance |
| `GetBySourceNameAndDateAsync` | Get logs within a date range (paginated) |
| `GetBySourceNameAndActionAsync` | Get logs by action type (Created, Updated, Deleted) |
| `GetBySourceNameActionAndDateAsync` | Get logs by action and date range |
| `GetSummaryBySourceNameAsync` | Get last event summary for each entity (optimized) |

See [Querying Audit Logs](./docs/querying-audit-logs.md) for complete documentation with examples.

---

## Documentation

See the [docs](./docs) folder for detailed documentation:

**Guides:**
- [Querying Audit Logs](./docs/querying-audit-logs.md) - Complete guide to IAuditQueryService

**Configuration by Stack:**
- [Dapper + SQL Server + JSON](./docs/dapper-sqlserver-json.md)
- [Dapper + SQL Server + XML](./docs/dapper-sqlserver-xml.md)
- [Dapper + PostgreSQL + JSON](./docs/dapper-postgresql-json.md)
- [Dapper + PostgreSQL + XML](./docs/dapper-postgresql-xml.md)
- [EF Core + SQL Server + JSON](./docs/efcore-sqlserver-json.md)
- [EF Core + SQL Server + XML](./docs/efcore-sqlserver-xml.md)
- [EF Core + PostgreSQL + JSON](./docs/efcore-postgresql-json.md)
- [EF Core + PostgreSQL + XML](./docs/efcore-postgresql-xml.md)

---

## Samples

The `samples` folder contains working examples:

- `AuditaX.Sample.Dapper` - Console app demonstrating Dapper integration
- `AuditaX.Sample.EntityFramework` - Console app demonstrating EF Core integration

---

## Versioning & .NET Support Policy

AuditaX follows a clear versioning strategy aligned with .NET's release cadence:

| AuditaX | .NET | C# | Status |
|---------|------|-----|--------|
| **1.x** | **.NET 10** | **C# 14** | **Current** |

### Future Support Policy

AuditaX will always support the **current LTS version** plus the **next standard release**:

| AuditaX | .NET | Notes |
|---------|------|-------|
| 1.x | .NET 10 | LTS only |
| 2.x | .NET 10 + .NET 11 | LTS + Standard |
| 3.x | .NET 12 | New LTS (drops .NET 10/11) |

---

## Requirements

- .NET 10.0 or later
- SQL Server 2016+ or PostgreSQL 12+

---

## License

[Apache 2.0 License](LICENSE)
