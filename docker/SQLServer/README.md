# AuditaX SQL Server Container

SQL Server 2022 Developer Edition container for AuditaX development.

## Quick Start

```bash
docker-compose up -d
```

## Connection Details

| Property          | Value                    |
|-------------------|--------------------------|
| Server            | localhost,1433           |
| Database          | AuditaX                  |
| User              | sa                       |
| Password          | sa                       |
| Trust Certificate | True                     |

**Connection String:**
```
Server=localhost,1433;Database=AuditaX;User Id=sa;Password=sa;TrustServerCertificate=True
```

## Container Information

| Property       | Value                                      |
|----------------|--------------------------------------------|
| Image          | mcr.microsoft.com/mssql/server:2022-latest |
| Container Name | auditax-sqlserver                          |
| Port           | 1433                                       |
| Volume         | auditax-sqlserver-data                     |
| Network        | auditax-sqlserver-network                  |

## Initialization Process

1. SQL Server starts with initial password `SQLServer@2022`
2. Health check verifies server is ready
3. Init container changes SA password to `sa`
4. Creates `AuditaX` database
5. Creates `Products` and `Customers` tables
6. Inserts sample data
7. Marks initialization complete (skips on subsequent starts)

## Commands

```bash
# Start
docker-compose up -d

# Stop
docker-compose down

# View logs
docker logs auditax-sqlserver

# Connect via sqlcmd
docker exec -it auditax-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P sa -C

# Reset (remove all data)
docker-compose down -v
```

## Database Schema

```sql
-- Products table
CREATE TABLE [dbo].[Products] (
    [ProductId] INT IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(256) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [Price] DECIMAL(18,2) NOT NULL,
    [Stock] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL
);

-- Customers table
CREATE TABLE [dbo].[Customers] (
    [CustomerId] INT IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(256) NOT NULL,
    [Email] NVARCHAR(256) NOT NULL,
    [Phone] NVARCHAR(50) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL
);
```

## appsettings.json Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=AuditaX;User Id=sa;Password=sa;TrustServerCertificate=True"
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
        "Key": "ProductId",
        "AuditProperties": ["Name", "Description", "Price", "Stock"]
      },
      "Customer": {
        "SourceName": "Customer",
        "Key": "CustomerId",
        "AuditProperties": ["Name", "Email", "Phone"]
      }
    }
  }
}
```
