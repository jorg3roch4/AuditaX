-- =============================================================================
-- AuditaX Sample: Entity Framework + SQL Server + XML
-- Script 01: Create Database
-- =============================================================================
-- Database: AuditaXEFSqlServerXml
-- =============================================================================

USE [master];
GO

-- Drop database if exists (for clean testing)
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'AuditaXEFSqlServerXml')
BEGIN
    ALTER DATABASE [AuditaXEFSqlServerXml] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [AuditaXEFSqlServerXml];
END
GO

-- Create database
CREATE DATABASE [AuditaXEFSqlServerXml];
GO

PRINT 'Database [AuditaXEFSqlServerXml] created successfully.';
GO
