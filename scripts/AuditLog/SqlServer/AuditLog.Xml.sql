-- =============================================================================
-- AuditaX - Audit Log Table for SQL Server (XML Format)
-- =============================================================================
-- This script creates the centralized audit table that stores change history
-- for all audited entities using XML format.
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

    -- Audit data: Complete change history in XML format
    [AuditLog]      XML                 NOT NULL,   -- XML document with all changes

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

PRINT 'Table [dbo].[AuditLog] created successfully (XML format).';
GO

-- =============================================================================
-- XML Format Example
-- =============================================================================
/*
The AuditLog column stores XML data like this:

<AuditLog>
  <Entry Action="Created" User="admin@example.com" Timestamp="2024-01-15T10:30:00Z" />
  <Entry Action="Updated" User="sales@example.com" Timestamp="2024-01-20T14:15:00Z">
    <Field Name="Price" Before="24.99" After="29.99" />
    <Field Name="Stock" Before="100" After="150" />
  </Entry>
  <Entry Action="Deleted" User="admin@example.com" Timestamp="2024-02-01T09:00:00Z" />
</AuditLog>

-- Query examples:

-- Get all entries for an entity
SELECT LogId, SourceName, SourceKey, AuditLog
FROM [dbo].[AuditLog]
WHERE SourceName = 'Product' AND SourceKey = '123';

-- Parse XML entries using XQuery
SELECT
    a.SourceName,
    a.SourceKey,
    e.value('@Action', 'NVARCHAR(50)') AS [Action],
    e.value('@User', 'NVARCHAR(256)') AS [User],
    e.value('@Timestamp', 'DATETIME2') AS [Timestamp]
FROM [dbo].[AuditLog] a
CROSS APPLY a.AuditLog.nodes('/AuditLog/Entry') AS T(e)
WHERE a.SourceName = 'Product';

-- Get field changes from Update entries
SELECT
    a.SourceName,
    a.SourceKey,
    e.value('@Action', 'NVARCHAR(50)') AS [Action],
    f.value('@Name', 'NVARCHAR(100)') AS FieldName,
    f.value('@Before', 'NVARCHAR(MAX)') AS BeforeValue,
    f.value('@After', 'NVARCHAR(MAX)') AS AfterValue
FROM [dbo].[AuditLog] a
CROSS APPLY a.AuditLog.nodes('/AuditLog/Entry') AS T(e)
CROSS APPLY e.nodes('Field') AS F(f)
WHERE a.SourceName = 'Product'
  AND e.value('@Action', 'NVARCHAR(50)') = 'Updated';
*/
