-- =============================================================================
-- AuditaX - SQL Server
-- Script 01: Create Products Table
-- =============================================================================

USE [AuditaX];
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
    DROP TABLE [dbo].[Products];
GO

CREATE TABLE [dbo].[Products]
(
    [Id]          INT             IDENTITY(1,1) NOT NULL,
    [Name]        NVARCHAR(200)   NOT NULL,
    [Description] NVARCHAR(1000)  NULL,
    [Price]       DECIMAL(18,2)   NOT NULL,
    [Stock]       INT             NOT NULL,
    [IsActive]    BIT             NOT NULL DEFAULT 1,
    CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

PRINT 'Table [dbo].[Products] created successfully.';
GO
