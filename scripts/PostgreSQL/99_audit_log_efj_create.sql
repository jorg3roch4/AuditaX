-- =============================================================================
-- AuditaX - PostgreSQL
-- Script 99: Create audit_log_efj Table (EF Core + JSON)
-- =============================================================================
-- ALTERNATIVO: Este script es para entornos de producción donde el usuario
-- no tenga permisos para crear tablas. En desarrollo, usar AutoCreateTable=true.
-- =============================================================================
-- Run this script connected to auditax database:
-- psql -U postgres -d auditax -f 99_audit_log_efj_create.sql
-- =============================================================================

DROP TABLE IF EXISTS public.audit_log_efj;

CREATE TABLE public.audit_log_efj
(
    log_id      UUID            NOT NULL DEFAULT gen_random_uuid(),
    source_name VARCHAR(64)     NOT NULL,
    source_key  VARCHAR(64)     NOT NULL,
    audit_log   JSONB           NOT NULL,
    CONSTRAINT pk_audit_log_efj PRIMARY KEY (log_id),
    CONSTRAINT uq_audit_log_efj_source UNIQUE (source_name, source_key)
);

CREATE INDEX ix_audit_log_efj_source_name ON public.audit_log_efj (source_name)
    INCLUDE (source_key, audit_log);

COMMENT ON TABLE public.audit_log_efj IS 'AuditaX audit log table for EF Core + JSON format';
