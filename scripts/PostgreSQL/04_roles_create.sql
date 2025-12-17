-- =============================================================================
-- AuditaX - PostgreSQL
-- Script 04: Create roles Table
-- =============================================================================
-- Run this script connected to auditax database:
-- psql -U postgres -d auditax -f 04_roles_create.sql
-- =============================================================================

DROP TABLE IF EXISTS public.roles CASCADE;

CREATE TABLE public.roles
(
    role_id     VARCHAR(450)    NOT NULL,
    role_name   VARCHAR(256)    NOT NULL,
    description VARCHAR(500)    NULL,
    CONSTRAINT pk_roles PRIMARY KEY (role_id)
);

COMMENT ON TABLE public.roles IS 'Sample roles table for Identity-like AuditaX demos';
