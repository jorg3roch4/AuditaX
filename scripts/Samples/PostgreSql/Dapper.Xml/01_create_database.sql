-- =============================================================================
-- AuditaX Sample: Dapper + PostgreSQL + XML
-- Script 01: Create Database
-- =============================================================================
-- Database: auditax_dapper_postgresql_xml
-- =============================================================================

-- Connect to postgres database first, then run this script
-- psql -h localhost -U postgres -d postgres -f 01_create_database.sql

-- Terminate existing connections
SELECT pg_terminate_backend(pg_stat_activity.pid)
FROM pg_stat_activity
WHERE pg_stat_activity.datname = 'auditax_dapper_postgresql_xml'
  AND pid <> pg_backend_pid();

-- Drop database if exists (for clean testing)
DROP DATABASE IF EXISTS auditax_dapper_postgresql_xml;

-- Create database
CREATE DATABASE auditax_dapper_postgresql_xml;

-- Success message
DO $$
BEGIN
    RAISE NOTICE 'Database auditax_dapper_postgresql_xml created successfully.';
END $$;
