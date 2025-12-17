-- =============================================================================
-- AuditaX - PostgreSQL
-- Script 02: Create product_tags Table
-- =============================================================================
-- Run this script connected to auditax database:
-- psql -U postgres -d auditax -f 02_product_tags_create.sql
-- =============================================================================

DROP TABLE IF EXISTS public.product_tags CASCADE;

CREATE TABLE public.product_tags
(
    id          SERIAL          NOT NULL,
    product_id  INT             NOT NULL,
    tag         VARCHAR(100)    NOT NULL,
    CONSTRAINT pk_product_tags PRIMARY KEY (id),
    CONSTRAINT fk_product_tags_products FOREIGN KEY (product_id)
        REFERENCES public.products (id) ON DELETE CASCADE
);

CREATE INDEX ix_product_tags_product_id ON public.product_tags (product_id);

COMMENT ON TABLE public.product_tags IS 'Product tags - related entity for AuditaX demos';
