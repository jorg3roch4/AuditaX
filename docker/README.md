# AuditaX Docker Development Environment

This directory contains Docker configurations for running SQL Server and PostgreSQL databases for AuditaX development and testing.

## Quick Start

### SQL Server

```bash
cd docker/SQLServer
docker-compose up -d
```

**Connection String:**
```
Server=localhost,1433;Database=AuditaX;User Id=sa;Password=sa;TrustServerCertificate=True
```

### PostgreSQL

```bash
cd docker/PostgreSQL
docker-compose up -d
```

**Connection String:**
```
Host=localhost;Port=5432;Database=auditax;Username=postgres;Password=postgres
```

## Directory Structure

```
docker/
├── SQLServer/
│   ├── docker-compose.yml      # SQL Server container configuration
│   └── init/
│       ├── 00-change-sa-password.sql   # Password change script
│       ├── 01-init-database.sh         # Initialization orchestrator
│       └── 02-create-database.sql      # Database and tables creation
│
├── PostgreSQL/
│   ├── docker-compose.yml      # PostgreSQL container configuration
│   └── init/
│       ├── 01-create-database.sql      # Database creation
│       └── 02-create-tables.sql        # Tables and sample data
│
└── README.md                   # This file
```

## Container Details

| Service    | Container Name      | Port | Credentials              |
|------------|---------------------|------|--------------------------|
| SQL Server | auditax-sqlserver   | 1433 | sa / sa                  |
| PostgreSQL | auditax-postgres    | 5432 | postgres / postgres      |

## Common Commands

### Start containers

```bash
# SQL Server
docker-compose -f docker/SQLServer/docker-compose.yml up -d

# PostgreSQL
docker-compose -f docker/PostgreSQL/docker-compose.yml up -d
```

### Stop containers

```bash
# SQL Server
docker-compose -f docker/SQLServer/docker-compose.yml down

# PostgreSQL
docker-compose -f docker/PostgreSQL/docker-compose.yml down
```

### View logs

```bash
# SQL Server
docker logs auditax-sqlserver

# PostgreSQL
docker logs auditax-postgres
```

### Reset database (remove volumes)

```bash
# SQL Server
docker-compose -f docker/SQLServer/docker-compose.yml down -v

# PostgreSQL
docker-compose -f docker/PostgreSQL/docker-compose.yml down -v
```

## Sample Tables

Both databases are initialized with the same sample tables for testing:

### Products Table

| Column      | SQL Server Type  | PostgreSQL Type  |
|-------------|------------------|------------------|
| ProductId   | INT IDENTITY     | SERIAL           |
| Name        | NVARCHAR(256)    | VARCHAR(256)     |
| Description | NVARCHAR(MAX)    | TEXT             |
| Price       | DECIMAL(18,2)    | DECIMAL(18,2)    |
| Stock       | INT              | INTEGER          |
| CreatedAt   | DATETIME2        | TIMESTAMPTZ      |
| UpdatedAt   | DATETIME2        | TIMESTAMPTZ      |

### Customers Table

| Column     | SQL Server Type  | PostgreSQL Type  |
|------------|------------------|------------------|
| CustomerId | INT IDENTITY     | SERIAL           |
| Name       | NVARCHAR(256)    | VARCHAR(256)     |
| Email      | NVARCHAR(256)    | VARCHAR(256)     |
| Phone      | NVARCHAR(50)     | VARCHAR(50)      |
| CreatedAt  | DATETIME2        | TIMESTAMPTZ      |
| UpdatedAt  | DATETIME2        | TIMESTAMPTZ      |

## Security Notice

These configurations use simple credentials for **development only**:
- SQL Server: `sa` / `sa`
- PostgreSQL: `postgres` / `postgres`

**Do NOT use these credentials in production!**

## Requirements

- Docker Desktop or Docker Engine
- Docker Compose v2+
- At least 2GB RAM available for containers

## Troubleshooting

### SQL Server takes too long to start

SQL Server requires more time to initialize on first run. The health check is configured to wait up to 5 minutes. Check logs with:

```bash
docker logs -f auditax-sqlserver
```

### Port already in use

If port 1433 or 5432 is already in use, either:
1. Stop the existing service
2. Change the port mapping in docker-compose.yml (e.g., `1434:1433`)

### Permission denied on Linux

If you get permission errors on init scripts, make them executable:

```bash
chmod +x docker/SQLServer/init/*.sh
```
