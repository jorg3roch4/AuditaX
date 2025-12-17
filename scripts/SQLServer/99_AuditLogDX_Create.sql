-- =============================================================================
-- AuditaX - SQL Server
-- Script 99: Create AuditLogDX Table (Dapper + XML)
-- =============================================================================
-- ALTERNATIVO: Este script es para entornos de producción donde el usuario
-- no tenga permisos para crear tablas. En desarrollo, usar AutoCreateTable=true.
-- =============================================================================

USE [AuditaX];
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogDX')
    DROP TABLE [dbo].[AuditLogDX];
GO

CREATE TABLE [dbo].[AuditLogDX]
(
    [Id]         BIGINT          IDENTITY(1,1) NOT NULL,
    [SourceName] NVARCHAR(200)   NOT NULL,
    [SourceKey]  NVARCHAR(200)   NOT NULL,
    [AuditLog]   XML             NOT NULL,  -- XML format
    [CreatedAt]  DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy]  NVARCHAR(200)   NULL,
    CONSTRAINT [PK_AuditLogDX] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogDX_SourceName_SourceKey]
    ON [dbo].[AuditLogDX] ([SourceName], [SourceKey]);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLogDX_CreatedAt]
    ON [dbo].[AuditLogDX] ([CreatedAt] DESC);
GO

PRINT 'Table [dbo].[AuditLogDX] created successfully.';
GO
