-- AuditaX Database Creation Script for SQL Server
-- Creates the AuditaX database with sample tables for testing

USE [master];
GO

-- Create AuditaX database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'AuditaX')
BEGIN
    CREATE DATABASE [AuditaX];
    PRINT 'Database AuditaX created successfully.';
END
ELSE
BEGIN
    PRINT 'Database AuditaX already exists.';
END
GO

USE [AuditaX];
GO

-- Configure database settings
ALTER DATABASE [AuditaX] SET COMPATIBILITY_LEVEL = 160;
ALTER DATABASE [AuditaX] SET RECOVERY FULL;
GO

-- Create Products table (sample entity for audit testing)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Products] (
        [ProductId] INT IDENTITY(1,1) PRIMARY KEY,
        [Name] NVARCHAR(256) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [Price] DECIMAL(18,2) NOT NULL,
        [Stock] INT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL
    );

    CREATE INDEX IX_Products_Name ON [dbo].[Products] ([Name]);
    PRINT 'Table Products created successfully.';
END
GO

-- Create Customers table (sample entity for audit testing)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Customers] (
        [CustomerId] INT IDENTITY(1,1) PRIMARY KEY,
        [Name] NVARCHAR(256) NOT NULL,
        [Email] NVARCHAR(256) NOT NULL,
        [Phone] NVARCHAR(50) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL
    );

    CREATE UNIQUE INDEX IX_Customers_Email ON [dbo].[Customers] ([Email]);
    PRINT 'Table Customers created successfully.';
END
GO

-- Insert sample data
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products])
BEGIN
    INSERT INTO [dbo].[Products] ([Name], [Description], [Price], [Stock])
    VALUES
        (N'Laptop Pro', N'High-performance laptop for professionals', 1299.99, 50),
        (N'Wireless Mouse', N'Ergonomic wireless mouse', 49.99, 200),
        (N'USB-C Hub', N'7-in-1 USB-C Hub with HDMI', 79.99, 150);
    PRINT 'Sample products inserted.';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Customers])
BEGIN
    INSERT INTO [dbo].[Customers] ([Name], [Email], [Phone])
    VALUES
        (N'John Doe', N'john.doe@example.com', N'+1-555-0100'),
        (N'Jane Smith', N'jane.smith@example.com', N'+1-555-0200');
    PRINT 'Sample customers inserted.';
END
GO

PRINT '============================================';
PRINT 'AuditaX database setup complete!';
PRINT '============================================';
GO
