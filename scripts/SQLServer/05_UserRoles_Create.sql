-- =============================================================================
-- AuditaX - SQL Server
-- Script 05: Create UserRoles Table
-- =============================================================================

USE [AuditaX];
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'UserRoles')
    DROP TABLE [dbo].[UserRoles];
GO

CREATE TABLE [dbo].[UserRoles]
(
    [UserId] NVARCHAR(450) NOT NULL,
    [RoleId] NVARCHAR(450) NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
    CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId])
        REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId])
        REFERENCES [dbo].[Roles] ([RoleId]) ON DELETE CASCADE
);
GO

PRINT 'Table [dbo].[UserRoles] created successfully.';
GO
