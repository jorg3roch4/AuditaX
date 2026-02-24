-- =============================================================================
-- AuditaX - SQL Server
-- Script 99: Create AuditLogDJ Table (Dapper + JSON)
-- =============================================================================
-- ALTERNATIVO: Este script es para entornos de producción donde el usuario
-- no tenga permisos para crear tablas. En desarrollo, usar AutoCreateTable=true.
-- =============================================================================

USE [AuditaX];
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogDJ' AND schema_id = SCHEMA_ID('dbo'))
    DROP TABLE [dbo].[AuditLogDJ];
GO

CREATE TABLE [dbo].[AuditLogDJ]
(
    [LogId]      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [SourceName] NVARCHAR(64)    NOT NULL,
    [SourceKey]  NVARCHAR(64)    NOT NULL,
    [AuditLog]   NVARCHAR(MAX)   NOT NULL,
    CONSTRAINT [PK_AuditLogDJ] PRIMARY KEY ([LogId]),
    CONSTRAINT [UQ_AuditLogDJ_Source] UNIQUE ([SourceName], [SourceKey])
);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogDJ_SourceName]
    ON [dbo].[AuditLogDJ] ([SourceName])
    INCLUDE ([SourceKey], [AuditLog]);
GO

PRINT 'Table [dbo].[AuditLogDJ] created successfully.';
GO
