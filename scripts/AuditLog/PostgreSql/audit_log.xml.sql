-- =============================================================================
-- AuditaX - Audit Log Table for PostgreSQL (XML Format)
-- =============================================================================
-- This script creates the centralized audit table that stores change history
-- for all audited entities using XML format.
--
-- Execute this script in your target database before using AuditaX.
-- =============================================================================

-- Drop existing table if exists (be careful in production!)
DROP TABLE IF EXISTS audit_log;

-- =============================================================================
-- Create audit_log Table
-- =============================================================================
CREATE TABLE audit_log
(
    -- Primary key: Unique identifier for each audit record
    log_id          UUID            NOT NULL DEFAULT gen_random_uuid(),

    -- Source identification: Entity type and primary key
    source_name     VARCHAR(50)     NOT NULL,   -- Entity type (e.g., "product", "order")
    source_key      VARCHAR(900)    NOT NULL,   -- Entity primary key value

    -- Audit data: Complete change history in XML format
    audit_log       XML             NOT NULL,   -- XML document with all changes

    -- Constraints
    CONSTRAINT pk_audit_log PRIMARY KEY (log_id),
    CONSTRAINT uq_audit_log_source UNIQUE (source_name, source_key)
);

-- =============================================================================
-- Create Indexes for Query Performance
-- =============================================================================

-- Index for querying by entity type
CREATE INDEX ix_audit_log_source_name ON audit_log (source_name);

-- Index for querying by entity key
CREATE INDEX ix_audit_log_source_key ON audit_log (source_key);

-- =============================================================================
-- Success Message
-- =============================================================================
DO $$
BEGIN
    RAISE NOTICE 'Table audit_log created successfully (XML format).';
END $$;

-- =============================================================================
-- XML Format Example
-- =============================================================================
/*
The audit_log column stores XML data like this:

<AuditLog>
  <Entry Action="Created" User="admin@example.com" Timestamp="2024-01-15T10:30:00Z" />
  <Entry Action="Updated" User="sales@example.com" Timestamp="2024-01-20T14:15:00Z">
    <Field Name="price" Before="24.99" After="29.99" />
    <Field Name="stock" Before="100" After="150" />
  </Entry>
  <Entry Action="Deleted" User="admin@example.com" Timestamp="2024-02-01T09:00:00Z" />
</AuditLog>

-- Query examples:

-- Get all entries for an entity
SELECT log_id, source_name, source_key, audit_log
FROM audit_log
WHERE source_name = 'product' AND source_key = '123';

-- Parse XML entries using xpath
SELECT
    a.source_name,
    a.source_key,
    (xpath('//@Action', e))[1]::text AS action,
    (xpath('//@User', e))[1]::text AS "user",
    (xpath('//@Timestamp', e))[1]::text AS "timestamp"
FROM audit_log a,
     unnest(xpath('/AuditLog/Entry', a.audit_log)) AS e
WHERE a.source_name = 'product';

-- Get field changes from Update entries
SELECT
    a.source_name,
    a.source_key,
    (xpath('//@Action', e))[1]::text AS action,
    (xpath('//@Name', f))[1]::text AS field_name,
    (xpath('//@Before', f))[1]::text AS before_value,
    (xpath('//@After', f))[1]::text AS after_value
FROM audit_log a,
     unnest(xpath('/AuditLog/Entry[@Action="Updated"]', a.audit_log)) AS e,
     unnest(xpath('Field', e)) AS f
WHERE a.source_name = 'product';
*/
