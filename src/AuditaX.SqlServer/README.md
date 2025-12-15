# AuditaX.SqlServer

[![NuGet](https://img.shields.io/nuget/v/AuditaX.SqlServer.svg?style=flat-square)](https://www.nuget.org/packages/AuditaX.SqlServer)[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg?style=flat-square)](https://github.com/jorg3roch4/AuditaX/blob/main/LICENSE)

SQL Server database provider for [AuditaX](https://github.com/jorg3roch4/AuditaX) audit logging library.

## Installation

```bash
dotnet add package AuditaX
dotnet add package AuditaX.SqlServer
dotnet add package AuditaX.Dapper             # OR AuditaX.EntityFramework
```

## Features

- SQL Server-specific SQL query generation
- Support for `NVARCHAR(MAX)` for change log storage
- `DATETIME2` for timestamps
- `IDENTITY` columns for auto-increment
- Compatible with SQL Server 2016+

## Configuration

### Service Registration

```csharp
services.AddAuditaX(configuration)
    .UseDapper<DapperContext>()       // OR .UseEntityFramework<AppDbContext>()
    .UseSqlServer()                   // Add SQL Server provider
    .ValidateOnStartup();
```

### Connection String

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=localhost;Initial Catalog=MyDatabase;User ID=sa;Password=YourPassword;TrustServerCertificate=True"
  }
}
```

### Recommended Configuration for SQL Server

```json
{
  "AuditaX": {
    "TableName": "AuditLog",
    "Schema": "dbo",
    "ChangeLogFormat": "Json",
    "AutoCreateTable": true,
    "EnableLogging": true
  }
}
```

## Generated Table Schema

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

## Querying Audit Logs

### Basic Query

```sql
SELECT * FROM AuditLog
WHERE SourceName = 'Product'
ORDER BY Timestamp DESC;
```

### Query JSON Changes

```sql
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

### Query XML Changes

```sql
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
```

## DapperContext for SQL Server

```csharp
using System.Data;
using Microsoft.Data.SqlClient;

public class DapperContext(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")!;

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
```

## Requirements

- SQL Server 2016 or later
- `Microsoft.Data.SqlClient` package (included as dependency)

## Documentation

For complete documentation, see the [main AuditaX repository](https://github.com/jorg3roch4/AuditaX).

- [Dapper + SQL Server + JSON](https://github.com/jorg3roch4/AuditaX/blob/main/docs/dapper-sqlserver-json.md)
- [Dapper + SQL Server + XML](https://github.com/jorg3roch4/AuditaX/blob/main/docs/dapper-sqlserver-xml.md)
- [EF Core + SQL Server + JSON](https://github.com/jorg3roch4/AuditaX/blob/main/docs/efcore-sqlserver-json.md)
- [EF Core + SQL Server + XML](https://github.com/jorg3roch4/AuditaX/blob/main/docs/efcore-sqlserver-xml.md)
