-- AuditaX Database Creation Script for PostgreSQL
-- Creates the AuditaX database with sample tables for testing

-- Create AuditaX database
SELECT 'Creating AuditaX database...' AS status;

CREATE DATABASE auditax
    WITH
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TEMPLATE = template0
    CONNECTION LIMIT = -1;

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE auditax TO postgres;

SELECT 'AuditaX database created successfully!' AS status;
