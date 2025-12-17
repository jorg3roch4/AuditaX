-- =============================================================================
-- AuditaX - SQL Server
-- Script 99: Create AuditLogEFJ Table (EF Core + JSON)
-- =============================================================================
-- ALTERNATIVO: Este script es para entornos de producción donde el usuario
-- no tenga permisos para crear tablas. En desarrollo, usar AutoCreateTable=true.
-- =============================================================================

USE [AuditaX];
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogEFJ')
    DROP TABLE [dbo].[AuditLogEFJ];
GO

CREATE TABLE [dbo].[AuditLogEFJ]
(
    [Id]         BIGINT          IDENTITY(1,1) NOT NULL,
    [SourceName] NVARCHAR(200)   NOT NULL,
    [SourceKey]  NVARCHAR(200)   NOT NULL,
    [AuditLog]   NVARCHAR(MAX)   NOT NULL,  -- JSON format
    [CreatedAt]  DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy]  NVARCHAR(200)   NULL,
    CONSTRAINT [PK_AuditLogEFJ] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogEFJ_SourceName_SourceKey]
    ON [dbo].[AuditLogEFJ] ([SourceName], [SourceKey]);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogEFJ_CreatedAt]
    ON [dbo].[AuditLogEFJ] ([CreatedAt] DESC);
GO

PRINT 'Table [dbo].[AuditLogEFJ] created successfully.';
GO
