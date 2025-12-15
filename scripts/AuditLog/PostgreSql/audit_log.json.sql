-- =============================================================================
-- AuditaX - Audit Log Table for PostgreSQL (JSON Format)
-- =============================================================================
-- This script creates the centralized audit table that stores change history
-- for all audited entities using JSON format.
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

    -- Audit data: Complete change history in JSON format
    audit_log       TEXT            NOT NULL,   -- JSON document with all changes

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
    RAISE NOTICE 'Table audit_log created successfully (JSON format).';
END $$;

-- =============================================================================
-- JSON Format Example
-- =============================================================================
/*
The audit_log column stores JSON data like this:

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
        { "name": "price", "before": "24.99", "after": "29.99" },
        { "name": "stock", "before": "100", "after": "150" }
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
SELECT log_id, source_name, source_key, audit_log
FROM audit_log
WHERE source_name = 'product' AND source_key = '123';

-- Parse JSON entries
SELECT
    a.source_name,
    a.source_key,
    e->>'action' AS action,
    e->>'user' AS "user",
    (e->>'timestamp')::timestamptz AS "timestamp"
FROM audit_log a,
     jsonb_array_elements(a.audit_log::jsonb->'entries') AS e
WHERE a.source_name = 'product';

-- Get field changes from Update entries
SELECT
    a.source_name,
    a.source_key,
    e->>'action' AS action,
    f->>'name' AS field_name,
    f->>'before' AS before_value,
    f->>'after' AS after_value
FROM audit_log a,
     jsonb_array_elements(a.audit_log::jsonb->'entries') AS e,
     jsonb_array_elements(e->'fields') AS f
WHERE a.source_name = 'product'
  AND e->>'action' = 'Updated';
*/
