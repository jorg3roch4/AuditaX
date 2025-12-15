-- =============================================================================
-- AuditaX Sample: Entity Framework + PostgreSQL + JSON
-- Script 02: Create Tables (products and product_tags)
-- =============================================================================
-- Database: auditax_ef_postgresql_json
-- Note: With Entity Framework, tables are typically created via EF migrations.
--       This script is provided for reference or manual setup.
-- Note: audit_log table is created automatically by AuditaX (AutoCreateTable=true)
--       or manually using scripts/AuditLog/PostgreSql/audit_log.json.sql
-- =============================================================================

-- Connect to the database first:
-- psql -h localhost -U postgres -d auditax_ef_postgresql_json -f 02_create_tables.sql

-- =============================================================================
-- products Table
-- =============================================================================
DROP TABLE IF EXISTS product_tags;
DROP TABLE IF EXISTS products;

CREATE TABLE products
(
    id          SERIAL          PRIMARY KEY,
    name        VARCHAR(200)    NOT NULL,
    description VARCHAR(1000)   NULL,
    price       DECIMAL(18,2)   NOT NULL,
    stock       INT             NOT NULL,
    is_active   BOOLEAN         NOT NULL DEFAULT TRUE
);

-- Success message
DO $$
BEGIN
    RAISE NOTICE 'Table products created successfully.';
END $$;

-- =============================================================================
-- product_tags Table (Related Entity - 1:N with products)
-- =============================================================================
CREATE TABLE product_tags
(
    id          SERIAL          PRIMARY KEY,
    product_id  INT             NOT NULL,
    tag         VARCHAR(100)    NOT NULL,
    CONSTRAINT fk_product_tags_products FOREIGN KEY (product_id)
        REFERENCES products (id) ON DELETE CASCADE
);

CREATE INDEX ix_product_tags_product_id ON product_tags (product_id);

-- Success message
DO $$
BEGIN
    RAISE NOTICE 'Table product_tags created successfully.';
END $$;
