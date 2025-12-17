-- =============================================================================
-- AuditaX - PostgreSQL
-- Script 03: Create users Table
-- =============================================================================
-- Run this script connected to auditax database:
-- psql -U postgres -d auditax -f 03_users_create.sql
-- =============================================================================

DROP TABLE IF EXISTS public.users CASCADE;

CREATE TABLE public.users
(
    user_id      VARCHAR(450)    NOT NULL,
    user_name    VARCHAR(256)    NOT NULL,
    email        VARCHAR(256)    NULL,
    phone_number VARCHAR(50)     NULL,
    is_active    BOOLEAN         NOT NULL DEFAULT TRUE,
    CONSTRAINT pk_users PRIMARY KEY (user_id)
);

COMMENT ON TABLE public.users IS 'Sample users table for Identity-like AuditaX demos';
