-- =============================================================================
-- AuditaX - SQL Server
-- Script 04: Create Roles Table
-- =============================================================================

USE [AuditaX];
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
    DROP TABLE [dbo].[Roles];
GO

CREATE TABLE [dbo].[Roles]
(
    [RoleId]      NVARCHAR(450)   NOT NULL,
    [RoleName]    NVARCHAR(256)   NOT NULL,
    [Description] NVARCHAR(500)   NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleId] ASC)
);
GO

PRINT 'Table [dbo].[Roles] created successfully.';
GO
