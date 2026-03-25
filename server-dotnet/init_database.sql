-- Markdown Notes App - Database Initialization Script
-- MySQL 5.7+

CREATE DATABASE IF NOT EXISTS markdown_notes CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE markdown_notes;

-- Users table
CREATE TABLE IF NOT EXISTS users (
    Id BIGINT NOT NULL PRIMARY KEY,
    Username VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Password VARCHAR(255) NOT NULL,
    CreatedAt DATETIME(6) NOT NULL,
    UpdatedAt DATETIME(6) NOT NULL,
    CreatedBy BIGINT NULL,
    UpdatedBy BIGINT NULL,
    Version BIGINT NOT NULL DEFAULT 0,
    UNIQUE INDEX IX_Users_Email (Email),
    UNIQUE INDEX IX_Users_Username (Username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Folders table
CREATE TABLE IF NOT EXISTS folders (
    Id BIGINT NOT NULL PRIMARY KEY,
    UserId BIGINT NOT NULL,
    Name VARCHAR(100) NOT NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    ParentId BIGINT NOT NULL DEFAULT 0,
    IsPinned BIT NOT NULL DEFAULT b'0',
    CreatedAt DATETIME(6) NOT NULL,
    UpdatedAt DATETIME(6) NOT NULL,
    CreatedBy BIGINT NULL,
    UpdatedBy BIGINT NULL,
    Version BIGINT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Folders_Users_UserId FOREIGN KEY (UserId) REFERENCES users (Id) ON DELETE CASCADE,
    INDEX IX_Folders_ParentId (ParentId),
    INDEX IX_Folders_UserId_IsPinned (UserId, IsPinned),
    INDEX IX_Folders_UserId_SortOrder (UserId, SortOrder)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Notes table
CREATE TABLE IF NOT EXISTS notes (
    Id BIGINT NOT NULL PRIMARY KEY,
    UserId BIGINT NOT NULL,
    FolderId BIGINT NULL,
    Title VARCHAR(255) NOT NULL,
    Content LONGTEXT NOT NULL,
    IsShared BIT NOT NULL DEFAULT b'0',
    ShareToken VARCHAR(255) NULL,
    IsDeleted BIT NOT NULL DEFAULT b'0',
    Version BIGINT NOT NULL DEFAULT 0,
    CreatedAt DATETIME(6) NOT NULL,
    UpdatedAt DATETIME(6) NOT NULL,
    CreatedBy BIGINT NULL,
    UpdatedBy BIGINT NULL,
    CONSTRAINT FK_Notes_Users_UserId FOREIGN KEY (UserId) REFERENCES users (Id) ON DELETE CASCADE,
    CONSTRAINT FK_Notes_Folders_FolderId FOREIGN KEY (FolderId) REFERENCES folders (Id) ON DELETE SET NULL,
    INDEX IX_Notes_ShareToken (ShareToken),
    INDEX IX_Notes_FolderId_UserId_IsDeleted (FolderId, UserId, IsDeleted),
    INDEX IX_Notes_UserId_IsDeleted (UserId, IsDeleted),
    INDEX IX_Notes_UpdatedAt (UpdatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Note Versions table
CREATE TABLE IF NOT EXISTS note_versions (
    Id BIGINT NOT NULL PRIMARY KEY,
    NoteId BIGINT NOT NULL,
    Title VARCHAR(255) NOT NULL,
    Content LONGTEXT NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT b'0',
    CreatedAt DATETIME(6) NOT NULL,
    CreatedBy BIGINT NULL,
    Version BIGINT NOT NULL DEFAULT 0,
    CONSTRAINT FK_NoteVersions_Notes_NoteId FOREIGN KEY (NoteId) REFERENCES notes (Id) ON DELETE CASCADE,
    INDEX IX_NoteVersions_NoteId (NoteId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
