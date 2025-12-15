-- =============================================================================
-- AuditaX Sample: Entity Framework + SQL Server + JSON
-- Script 01: Create Database
-- =============================================================================
-- Database: AuditaXEFSqlServerJson
-- =============================================================================

USE [master];
GO

-- Drop database if exists (for clean testing)
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'AuditaXEFSqlServerJson')
BEGIN
    ALTER DATABASE [AuditaXEFSqlServerJson] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [AuditaXEFSqlServerJson];
END
GO

-- Create database
CREATE DATABASE [AuditaXEFSqlServerJson];
GO

PRINT 'Database [AuditaXEFSqlServerJson] created successfully.';
GO
