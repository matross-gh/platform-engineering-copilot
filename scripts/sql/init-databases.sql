-- Initialize Platform Engineering Copilot Databases
-- This script creates the necessary databases for the application

USE master;
GO

-- Create Platform API Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SupervisorPlatformDb')
BEGIN
    CREATE DATABASE SupervisorPlatformDb;
    PRINT 'Created SupervisorPlatformDb database';
END
GO

-- Create Chat Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SupervisorPlatformChatDb')
BEGIN
    CREATE DATABASE SupervisorPlatformChatDb;
    PRINT 'Created SupervisorPlatformChatDb database';
END
GO

-- Create Admin Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SupervisorAdminDb')
BEGIN
    CREATE DATABASE SupervisorAdminDb;
    PRINT 'Created SupervisorAdminDb database';
END
GO

PRINT 'Database initialization complete';
GO
