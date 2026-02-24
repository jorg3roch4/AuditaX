-- =============================================================================
-- AuditaX - PostgreSQL
-- Script 99: Create audit_log_dj Table (Dapper + JSON)
-- =============================================================================
-- ALTERNATIVO: Este script es para entornos de producción donde el usuario
-- no tenga permisos para crear tablas. En desarrollo, usar AutoCreateTable=true.
-- =============================================================================
-- Run this script connected to auditax database:
-- psql -U postgres -d auditax -f 99_audit_log_dj_create.sql
-- =============================================================================

DROP TABLE IF EXISTS public.audit_log_dj;

CREATE TABLE public.audit_log_dj
(
    log_id      UUID            NOT NULL DEFAULT gen_random_uuid(),
    source_name VARCHAR(64)     NOT NULL,
    source_key  VARCHAR(64)     NOT NULL,
    audit_log   JSONB           NOT NULL,
    CONSTRAINT pk_audit_log_dj PRIMARY KEY (log_id),
    CONSTRAINT uq_audit_log_dj_source UNIQUE (source_name, source_key)
);

CREATE INDEX ix_audit_log_dj_source_name ON public.audit_log_dj (source_name)
    INCLUDE (source_key, audit_log);

COMMENT ON TABLE public.audit_log_dj IS 'AuditaX audit log table for Dapper + JSON format';
