-- =============================================================================
-- AuditaX Sample: Dapper + SQL Server + XML
-- Script 01: Create Database
-- =============================================================================
-- Database: AuditaXDapperSqlServerXml
-- =============================================================================

USE [master];
GO

-- Drop database if exists (for clean testing)
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'AuditaXDapperSqlServerXml')
BEGIN
    ALTER DATABASE [AuditaXDapperSqlServerXml] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [AuditaXDapperSqlServerXml];
END
GO

-- Create database
CREATE DATABASE [AuditaXDapperSqlServerXml];
GO

PRINT 'Database [AuditaXDapperSqlServerXml] created successfully.';
GO
