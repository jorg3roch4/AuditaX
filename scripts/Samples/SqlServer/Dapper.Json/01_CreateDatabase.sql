-- =============================================================================
-- AuditaX Sample: Dapper + SQL Server + JSON
-- Script 01: Create Database
-- =============================================================================
-- Database: AuditaXDapperSqlServerJson
-- =============================================================================

USE [master];
GO

-- Drop database if exists (for clean testing)
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'AuditaXDapperSqlServerJson')
BEGIN
    ALTER DATABASE [AuditaXDapperSqlServerJson] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [AuditaXDapperSqlServerJson];
END
GO

-- Create database
CREATE DATABASE [AuditaXDapperSqlServerJson];
GO

PRINT 'Database [AuditaXDapperSqlServerJson] created successfully.';
GO
