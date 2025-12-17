-- =============================================================================
-- AuditaX - SQL Server
-- Script 99: Create AuditLogDJ Table (Dapper + JSON)
-- =============================================================================
-- ALTERNATIVO: Este script es para entornos de producción donde el usuario
-- no tenga permisos para crear tablas. En desarrollo, usar AutoCreateTable=true.
-- =============================================================================

USE [AuditaX];
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogDJ')
    DROP TABLE [dbo].[AuditLogDJ];
GO

CREATE TABLE [dbo].[AuditLogDJ]
(
    [Id]         BIGINT          IDENTITY(1,1) NOT NULL,
    [SourceName] NVARCHAR(200)   NOT NULL,
    [SourceKey]  NVARCHAR(200)   NOT NULL,
    [AuditLog]   NVARCHAR(MAX)   NOT NULL,  -- JSON format
    [CreatedAt]  DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy]  NVARCHAR(200)   NULL,
    CONSTRAINT [PK_AuditLogDJ] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogDJ_SourceName_SourceKey]
    ON [dbo].[AuditLogDJ] ([SourceName], [SourceKey]);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogDJ_CreatedAt]
    ON [dbo].[AuditLogDJ] ([CreatedAt] DESC);
GO

PRINT 'Table [dbo].[AuditLogDJ] created successfully.';
GO
