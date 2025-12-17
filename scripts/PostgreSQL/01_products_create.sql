-- =============================================================================
-- AuditaX - PostgreSQL
-- Script 01: Create products Table
-- =============================================================================
-- Run this script connected to auditax database:
-- psql -U postgres -d auditax -f 01_products_create.sql
-- =============================================================================

DROP TABLE IF EXISTS public.products CASCADE;

CREATE TABLE public.products
(
    id          SERIAL          NOT NULL,
    name        VARCHAR(200)    NOT NULL,
    description VARCHAR(1000)   NULL,
    price       DECIMAL(18,2)   NOT NULL,
    stock       INT             NOT NULL,
    is_active   BOOLEAN         NOT NULL DEFAULT TRUE,
    CONSTRAINT pk_products PRIMARY KEY (id)
);

COMMENT ON TABLE public.products IS 'Sample products table for AuditaX demos';
