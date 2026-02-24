-- =============================================================================
-- AuditaX - SQL Server
-- Script 99: Create AuditLogDX Table (Dapper + XML)
-- =============================================================================
-- ALTERNATIVO: Este script es para entornos de producción donde el usuario
-- no tenga permisos para crear tablas. En desarrollo, usar AutoCreateTable=true.
-- =============================================================================

USE [AuditaX];
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogDX' AND schema_id = SCHEMA_ID('dbo'))
    DROP TABLE [dbo].[AuditLogDX];
GO

CREATE TABLE [dbo].[AuditLogDX]
(
    [LogId]      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [SourceName] NVARCHAR(64)    NOT NULL,
    [SourceKey]  NVARCHAR(64)    NOT NULL,
    [AuditLog]   XML             NOT NULL,
    CONSTRAINT [PK_AuditLogDX] PRIMARY KEY ([LogId]),
    CONSTRAINT [UQ_AuditLogDX_Source] UNIQUE ([SourceName], [SourceKey])
);
GO

-- Note: XML columns cannot be used in INCLUDE clause; index covers SourceName and SourceKey only
CREATE NONCLUSTERED INDEX [IX_AuditLogDX_SourceName]
    ON [dbo].[AuditLogDX] ([SourceName])
    INCLUDE ([SourceKey]);
GO

PRINT 'Table [dbo].[AuditLogDX] created successfully.';
GO
