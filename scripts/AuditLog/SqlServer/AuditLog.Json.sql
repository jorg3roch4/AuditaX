-- =============================================================================
-- AuditaX - Audit Log Table for SQL Server (JSON Format)
-- =============================================================================
-- This script creates the centralized audit table that stores change history
-- for all audited entities using JSON format.
--
-- Execute this script in your target database before using AuditaX.
-- =============================================================================

-- Drop existing table if exists (be careful in production!)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLog' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE [dbo].[AuditLog];
    PRINT 'Existing [dbo].[AuditLog] table dropped.';
END
GO

-- =============================================================================
-- Create AuditLog Table
-- =============================================================================
CREATE TABLE [dbo].[AuditLog]
(
    -- Primary key: Unique identifier for each audit record
    [LogId]         UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),

    -- Source identification: Entity type and primary key
    [SourceName]    NVARCHAR(50)        NOT NULL,   -- Entity type (e.g., "Product", "Order")
    [SourceKey]     NVARCHAR(900)       NOT NULL,   -- Entity primary key value

    -- Audit data: Complete change history in JSON format
    [AuditLog]      NVARCHAR(MAX)       NOT NULL,   -- JSON document with all changes

    -- Constraints
    CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([LogId] ASC),
    CONSTRAINT [UQ_AuditLog_Source] UNIQUE ([SourceName], [SourceKey])
);
GO

-- =============================================================================
-- Create Indexes for Query Performance
-- =============================================================================

-- Index for querying by entity type
CREATE NONCLUSTERED INDEX [IX_AuditLog_SourceName]
    ON [dbo].[AuditLog] ([SourceName])
    INCLUDE ([SourceKey]);
GO

-- Index for querying by entity key
CREATE NONCLUSTERED INDEX [IX_AuditLog_SourceKey]
    ON [dbo].[AuditLog] ([SourceKey])
    INCLUDE ([SourceName]);
GO

PRINT 'Table [dbo].[AuditLog] created successfully (JSON format).';
GO

-- =============================================================================
-- JSON Format Example
-- =============================================================================
/*
The AuditLog column stores JSON data like this:

{
  "entries": [
    {
      "action": "Created",
      "user": "admin@example.com",
      "timestamp": "2024-01-15T10:30:00Z",
      "fields": []
    },
    {
      "action": "Updated",
      "user": "sales@example.com",
      "timestamp": "2024-01-20T14:15:00Z",
      "fields": [
        { "name": "Price", "before": "24.99", "after": "29.99" },
        { "name": "Stock", "before": "100", "after": "150" }
      ]
    },
    {
      "action": "Deleted",
      "user": "admin@example.com",
      "timestamp": "2024-02-01T09:00:00Z",
      "fields": []
    }
  ]
}

-- Query examples:

-- Get all entries for an entity
SELECT LogId, SourceName, SourceKey, AuditLog
FROM [dbo].[AuditLog]
WHERE SourceName = 'Product' AND SourceKey = '123';

-- Parse JSON entries (SQL Server 2016+)
SELECT
    a.SourceName,
    a.SourceKey,
    e.[action],
    e.[user],
    e.[timestamp]
FROM [dbo].[AuditLog] a
CROSS APPLY OPENJSON(a.AuditLog, '$.entries')
WITH (
    [action] NVARCHAR(50) '$.action',
    [user] NVARCHAR(256) '$.user',
    [timestamp] DATETIME2 '$.timestamp'
) e
WHERE a.SourceName = 'Product';
*/
