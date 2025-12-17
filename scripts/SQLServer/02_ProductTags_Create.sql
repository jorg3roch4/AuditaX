-- =============================================================================
-- AuditaX - SQL Server
-- Script 02: Create ProductTags Table
-- =============================================================================

USE [AuditaX];
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductTags')
    DROP TABLE [dbo].[ProductTags];
GO

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
