-- =============================================================================
-- AuditaX - PostgreSQL
-- Script 05: Create user_roles Table
-- =============================================================================
-- Run this script connected to auditax database:
-- psql -U postgres -d auditax -f 05_user_roles_create.sql
-- =============================================================================

DROP TABLE IF EXISTS public.user_roles CASCADE;

CREATE TABLE public.user_roles
(
    user_id VARCHAR(450) NOT NULL,
    role_id VARCHAR(450) NOT NULL,
    CONSTRAINT pk_user_roles PRIMARY KEY (user_id, role_id),
    CONSTRAINT fk_user_roles_users FOREIGN KEY (user_id)
        REFERENCES public.users (user_id) ON DELETE CASCADE,
    CONSTRAINT fk_user_roles_roles FOREIGN KEY (role_id)
        REFERENCES public.roles (role_id) ON DELETE CASCADE
);

COMMENT ON TABLE public.user_roles IS 'User-Role junction table for Identity-like AuditaX demos';
