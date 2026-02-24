-- =============================================================================
-- AuditaX - SQL Server
-- Script 99: Create AuditLogEFJ Table (EF Core + JSON)
-- =============================================================================
-- ALTERNATIVO: Este script es para entornos de producción donde el usuario
-- no tenga permisos para crear tablas. En desarrollo, usar AutoCreateTable=true.
-- =============================================================================

USE [AuditaX];
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogEFJ' AND schema_id = SCHEMA_ID('dbo'))
    DROP TABLE [dbo].[AuditLogEFJ];
GO

CREATE TABLE [dbo].[AuditLogEFJ]
(
    [LogId]      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [SourceName] NVARCHAR(64)    NOT NULL,
    [SourceKey]  NVARCHAR(64)    NOT NULL,
    [AuditLog]   NVARCHAR(MAX)   NOT NULL,
    CONSTRAINT [PK_AuditLogEFJ] PRIMARY KEY ([LogId]),
    CONSTRAINT [UQ_AuditLogEFJ_Source] UNIQUE ([SourceName], [SourceKey])
);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogEFJ_SourceName]
    ON [dbo].[AuditLogEFJ] ([SourceName])
    INCLUDE ([SourceKey], [AuditLog]);
GO

PRINT 'Table [dbo].[AuditLogEFJ] created successfully.';
GO
