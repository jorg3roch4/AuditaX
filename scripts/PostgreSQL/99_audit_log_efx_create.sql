-- =============================================================================
-- AuditaX - PostgreSQL
-- Script 99: Create audit_log_efx Table (EF Core + XML)
-- =============================================================================
-- ALTERNATIVO: Este script es para entornos de producción donde el usuario
-- no tenga permisos para crear tablas. En desarrollo, usar AutoCreateTable=true.
-- =============================================================================
-- Run this script connected to auditax database:
-- psql -U postgres -d auditax -f 99_audit_log_efx_create.sql
-- =============================================================================

DROP TABLE IF EXISTS public.audit_log_efx;

CREATE TABLE public.audit_log_efx
(
    id          BIGSERIAL       NOT NULL,
    source_name VARCHAR(200)    NOT NULL,
    source_key  VARCHAR(200)    NOT NULL,
    audit_log   XML             NOT NULL,
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(200)    NULL,
    CONSTRAINT pk_audit_log_efx PRIMARY KEY (id)
);

CREATE INDEX ix_audit_log_efx_source ON public.audit_log_efx (source_name, source_key);
CREATE INDEX ix_audit_log_efx_created_at ON public.audit_log_efx (created_at DESC);

COMMENT ON TABLE public.audit_log_efx IS 'AuditaX audit log table for EF Core + XML format';
