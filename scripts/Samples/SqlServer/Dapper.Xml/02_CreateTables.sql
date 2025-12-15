-- =============================================================================
-- AuditaX Sample: Dapper + SQL Server + XML
-- Script 02: Create Tables (Products and ProductTags)
-- =============================================================================
-- Database: AuditaXDapperSqlServerXml
-- Note: AuditLog table is created automatically by AuditaX (AutoCreateTable=true)
--       or manually using scripts/AuditLog/SqlServer/AuditLog.Xml.sql
-- =============================================================================

USE [AuditaXDapperSqlServerXml];
GO

-- =============================================================================
-- Products Table
-- =============================================================================
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductTags')
    DROP TABLE [dbo].[ProductTags];
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

-- =============================================================================
-- ProductTags Table (Related Entity - 1:N with Products)
-- =============================================================================
CREATE TABLE [dbo].[ProductTags]
(
    [Id]        INT             IDENTITY(1,1) NOT NULL,
    [ProductId] INT             NOT NULL,
    [Tag]       NVARCHAR(100)   NOT NULL,
    CONSTRAINT [PK_ProductTags] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ProductTags_Products] FOREIGN KEY ([ProductId])
        REFERENCES [dbo].[Products] ([Id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_ProductTags_ProductId]
    ON [dbo].[ProductTags] ([ProductId]);
GO

PRINT 'Table [dbo].[ProductTags] created successfully.';
GO
