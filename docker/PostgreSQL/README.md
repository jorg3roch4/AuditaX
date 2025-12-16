# AuditaX PostgreSQL Container

PostgreSQL container for AuditaX development.

## Quick Start

```bash
docker-compose up -d
```

## Connection Details

| Property | Value     |
|----------|-----------|
| Host     | localhost |
| Port     | 5432      |
| Database | auditax   |
| User     | postgres  |
| Password | postgres  |

**Connection String:**
```
Host=localhost;Port=5432;Database=auditax;Username=postgres;Password=postgres
```

## Container Information

| Property       | Value                   |
|----------------|-------------------------|
| Image          | postgres:latest         |
| Container Name | auditax-postgres        |
| Port           | 5432                    |
| Volume         | auditax-postgres-data   |
| Network        | auditax-postgres-network|

## Initialization Process

1. PostgreSQL starts with default credentials
2. Creates `auditax` database
3. Creates `products` and `customers` tables
4. Inserts sample data
5. Initialization scripts run only on first start

## Commands

```bash
# Start
docker-compose up -d

# Stop
docker-compose down

# View logs
docker logs auditax-postgres

# Connect via psql
docker exec -it auditax-postgres psql -U postgres -d auditax

# Reset (remove all data)
docker-compose down -v
```

## Database Schema

```sql
-- Products table
CREATE TABLE products (
    product_id SERIAL PRIMARY KEY,
    name VARCHAR(256) NOT NULL,
    description TEXT,
    price DECIMAL(18,2) NOT NULL,
    stock INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

-- Customers table
CREATE TABLE customers (
    customer_id SERIAL PRIMARY KEY,
    name VARCHAR(256) NOT NULL,
    email VARCHAR(256) NOT NULL,
    phone VARCHAR(50),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);
```

## appsettings.json Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=auditax;Username=postgres;Password=postgres"
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
