-- =============================================================================
-- AuditaX - SQL Server Database Setup
-- Script 00: Create Database
-- =============================================================================

USE [master];
GO

-- Drop database if exists
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'AuditaX')
BEGIN
    ALTER DATABASE [AuditaX] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [AuditaX];
END
GO

-- Create database
CREATE DATABASE [AuditaX];
GO

USE [AuditaX];
GO

PRINT 'Database [AuditaX] created successfully.';
GO
