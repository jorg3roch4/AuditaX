-- =============================================================================
-- AuditaX - SQL Server
-- Script 03: Create Users Table
-- =============================================================================

USE [AuditaX];
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
    DROP TABLE [dbo].[Users];
GO

CREATE TABLE [dbo].[Users]
(
    [UserId]      NVARCHAR(450)   NOT NULL,
    [UserName]    NVARCHAR(256)   NOT NULL,
    [Email]       NVARCHAR(256)   NULL,
    [PhoneNumber] NVARCHAR(50)    NULL,
    [IsActive]    BIT             NOT NULL DEFAULT 1,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserId] ASC)
);
GO

PRINT 'Table [dbo].[Users] created successfully.';
GO
