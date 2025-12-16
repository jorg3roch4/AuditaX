# SQL Scripts

This folder contains SQL scripts for setting up AuditaX and sample databases.

## Structure

```
scripts/
├── AuditLog/           # Audit log table scripts
│   ├── SqlServer/
│   │   └── AuditLog_Create.sql
│   └── PostgreSql/
│       └── audit_log_create.sql
│
└── Samples/            # Sample database scripts
    ├── SqlServer/
    │   └── Samples_Create.sql
    └── PostgreSql/
        └── samples_create.sql
```

## AuditLog Scripts

These scripts create the audit log table. Note that AuditaX can create the table automatically if `AutoCreateTable: true` is set in configuration.

### SQL Server

```sql
-- scripts/AuditLog/SqlServer/AuditLog.Json.sql (or AuditLog.Xml.sql)
CREATE TABLE [dbo].[AuditLog] (
    [LogId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [SourceName] NVARCHAR(50) NOT NULL,
    [SourceKey] NVARCHAR(900) NOT NULL,
    [AuditLog] NVARCHAR(MAX) NOT NULL,  -- Use XML type for XML format
    CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([LogId]),
    CONSTRAINT [UQ_AuditLog_Source] UNIQUE ([SourceName], [SourceKey])
);

CREATE INDEX IX_AuditLog_SourceName ON [dbo].[AuditLog] ([SourceName]) INCLUDE ([SourceKey]);
CREATE INDEX IX_AuditLog_SourceKey ON [dbo].[AuditLog] ([SourceKey]) INCLUDE ([SourceName]);
```

### PostgreSQL

```sql
-- scripts/AuditLog/PostgreSql/audit_log.json.sql
CREATE TABLE IF NOT EXISTS public.audit_log (
    log_id UUID NOT NULL DEFAULT gen_random_uuid(),
    source_name VARCHAR(50) NOT NULL,
    source_key VARCHAR(900) NOT NULL,
    audit_log JSONB NOT NULL,  -- Use XML type for XML format
    CONSTRAINT pk_audit_log PRIMARY KEY (log_id),
    CONSTRAINT uq_audit_log_source UNIQUE (source_name, source_key)
);

CREATE INDEX IF NOT EXISTS ix_audit_log_source_name ON public.audit_log (source_name);
CREATE INDEX IF NOT EXISTS ix_audit_log_source_key ON public.audit_log (source_key);
```

## Sample Database Scripts

These scripts create sample tables used by the sample applications.

### SQL Server

```sql
-- scripts/Samples/SqlServer/Samples_Create.sql
CREATE DATABASE AuditaXSamples;
GO

USE AuditaXSamples;
GO

CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(256) NOT NULL,
    Description NVARCHAR(MAX),
    Price DECIMAL(18,2) NOT NULL,
    Stock INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);
```

### PostgreSQL

```sql
-- scripts/Samples/PostgreSql/samples_create.sql
CREATE DATABASE auditax_samples;

\c auditax_samples

CREATE TABLE products (
    product_id SERIAL PRIMARY KEY,
    name VARCHAR(256) NOT NULL,
    description TEXT,
    price DECIMAL(18,2) NOT NULL,
    stock INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);
```

## Usage

### SQL Server
```bash
sqlcmd -S localhost -U sa -P YourPassword -i scripts/Samples/SqlServer/Samples_Create.sql
```

### PostgreSQL
```bash
psql -U postgres -f scripts/Samples/PostgreSql/samples_create.sql
```

## Notes

- The audit log table is created automatically by AuditaX when `AutoCreateTable: true`
- Sample scripts are provided for manual setup or testing
- Adjust schema names and connection parameters as needed
