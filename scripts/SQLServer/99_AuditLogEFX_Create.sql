-- =============================================================================
-- AuditaX - SQL Server
-- Script 99: Create AuditLogEFX Table (EF Core + XML)
-- =============================================================================
-- ALTERNATIVO: Este script es para entornos de producción donde el usuario
-- no tenga permisos para crear tablas. En desarrollo, usar AutoCreateTable=true.
-- =============================================================================

USE [AuditaX];
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogEFX')
    DROP TABLE [dbo].[AuditLogEFX];
GO

CREATE TABLE [dbo].[AuditLogEFX]
(
    [Id]         BIGINT          IDENTITY(1,1) NOT NULL,
    [SourceName] NVARCHAR(200)   NOT NULL,
    [SourceKey]  NVARCHAR(200)   NOT NULL,
    [AuditLog]   XML             NOT NULL,  -- XML format
    [CreatedAt]  DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy]  NVARCHAR(200)   NULL,
    CONSTRAINT [PK_AuditLogEFX] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogEFX_SourceName_SourceKey]
    ON [dbo].[AuditLogEFX] ([SourceName], [SourceKey]);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogEFX_CreatedAt]
    ON [dbo].[AuditLogEFX] ([CreatedAt] DESC);
GO

PRINT 'Table [dbo].[AuditLogEFX] created successfully.';
GO
