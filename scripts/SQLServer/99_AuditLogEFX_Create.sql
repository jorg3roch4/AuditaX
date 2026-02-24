-- =============================================================================
-- AuditaX - SQL Server
-- Script 99: Create AuditLogEFX Table (EF Core + XML)
-- =============================================================================
-- ALTERNATIVO: Este script es para entornos de producción donde el usuario
-- no tenga permisos para crear tablas. En desarrollo, usar AutoCreateTable=true.
-- =============================================================================

USE [AuditaX];
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogEFX' AND schema_id = SCHEMA_ID('dbo'))
    DROP TABLE [dbo].[AuditLogEFX];
GO

CREATE TABLE [dbo].[AuditLogEFX]
(
    [LogId]      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [SourceName] NVARCHAR(64)    NOT NULL,
    [SourceKey]  NVARCHAR(64)    NOT NULL,
    [AuditLog]   XML             NOT NULL,
    CONSTRAINT [PK_AuditLogEFX] PRIMARY KEY ([LogId]),
    CONSTRAINT [UQ_AuditLogEFX_Source] UNIQUE ([SourceName], [SourceKey])
);
GO

-- Note: XML columns cannot be used in INCLUDE clause; index covers SourceName and SourceKey only
CREATE NONCLUSTERED INDEX [IX_AuditLogEFX_SourceName]
    ON [dbo].[AuditLogEFX] ([SourceName])
    INCLUDE ([SourceKey]);
GO

PRINT 'Table [dbo].[AuditLogEFX] created successfully.';
GO
