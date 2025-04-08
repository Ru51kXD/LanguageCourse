-- Создание базы данных
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'LanguageCourses_New')
BEGIN
    CREATE DATABASE LanguageCourses_New;
END
GO

USE LanguageCourses_New;
GO

-- Создание таблиц
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Languages')
BEGIN
    CREATE TABLE Languages (
        Id NVARCHAR(36) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Code NVARCHAR(10) NOT NULL,
        CreatedAt DATETIME NOT NULL,
        UpdatedAt DATETIME NOT NULL
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Levels')
BEGIN
    CREATE TABLE Levels (
        Id NVARCHAR(36) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(MAX),
        CreatedAt DATETIME NOT NULL,
        UpdatedAt DATETIME NOT NULL
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Videos')
BEGIN
    CREATE TABLE Videos (
        Id NVARCHAR(36) PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX),
        VideoUrl NVARCHAR(500) NOT NULL,
        ThumbnailUrl NVARCHAR(500),
        LanguageId NVARCHAR(36) NOT NULL,
        LevelId NVARCHAR(36) NOT NULL,
        IsFeatured BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME NOT NULL,
        UpdatedAt DATETIME NOT NULL,
        FOREIGN KEY (LanguageId) REFERENCES Languages(Id),
        FOREIGN KEY (LevelId) REFERENCES Levels(Id)
    );
END

-- Вставка начальных данных
-- Языки
INSERT INTO Languages (Id, Name, Code, CreatedAt, UpdatedAt) VALUES
('1', 'Английский', 'en', GETDATE(), GETDATE()),
('2', 'Немецкий', 'de', GETDATE(), GETDATE()),
('3', 'Французский', 'fr', GETDATE(), GETDATE());

-- Уровни
INSERT INTO Levels (Id, Name, Description, CreatedAt, UpdatedAt) VALUES
('1', 'Начальный', 'Для начинающих', GETDATE(), GETDATE()),
('2', 'Средний', 'Для продолжающих', GETDATE(), GETDATE()),
('3', 'Продвинутый', 'Для продвинутых', GETDATE(), GETDATE());

-- Видео
-- Здесь будут вставлены данные из вашей текущей базы данных
-- Скопируйте сюда результаты выполнения скрипта экспорта данных 