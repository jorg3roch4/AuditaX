-- =============================================================================
-- AuditaX - PostgreSQL Database Setup
-- Script 00: Create Database
-- =============================================================================
-- Run this script as postgres superuser:
-- psql -U postgres -f 00_auditax_create.sql
-- =============================================================================

-- Terminate existing connections
SELECT pg_terminate_backend(pg_stat_activity.pid)
FROM pg_stat_activity
WHERE pg_stat_activity.datname = 'auditax'
AND pid <> pg_backend_pid();

-- Drop database if exists
DROP DATABASE IF EXISTS auditax;

-- Create database
CREATE DATABASE auditax;

-- Connect to the new database for subsequent scripts
-- \c auditax
