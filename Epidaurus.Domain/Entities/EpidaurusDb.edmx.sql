
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, and Azure
-- --------------------------------------------------
-- Date Created: 11/10/2011 22:09:40
-- Generated from EDMX file: C:\Dev\Web\Epidaurus\Models\EpidaurusDb.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [epidaurus];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK_MovieSeenStatus]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[SeenStatuses] DROP CONSTRAINT [FK_MovieSeenStatus];
GO
IF OBJECT_ID(N'[dbo].[FK_StorageLocationMovieSource]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[MovieAtStorages] DROP CONSTRAINT [FK_StorageLocationMovieSource];
GO
IF OBJECT_ID(N'[dbo].[FK_UserSeenStatus]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[SeenStatuses] DROP CONSTRAINT [FK_UserSeenStatus];
GO
IF OBJECT_ID(N'[dbo].[FK_MovieMovieAtStorage]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[MovieAtStorages] DROP CONSTRAINT [FK_MovieMovieAtStorage];
GO
IF OBJECT_ID(N'[dbo].[FK_GenreMovie_Genre]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[GenreMovie] DROP CONSTRAINT [FK_GenreMovie_Genre];
GO
IF OBJECT_ID(N'[dbo].[FK_GenreMovie_Movie]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[GenreMovie] DROP CONSTRAINT [FK_GenreMovie_Movie];
GO
IF OBJECT_ID(N'[dbo].[FK_MovieWriter_Movie]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[MovieWriter] DROP CONSTRAINT [FK_MovieWriter_Movie];
GO
IF OBJECT_ID(N'[dbo].[FK_MovieWriter_Person]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[MovieWriter] DROP CONSTRAINT [FK_MovieWriter_Person];
GO
IF OBJECT_ID(N'[dbo].[FK_MovieActor_Movie]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[MovieActor] DROP CONSTRAINT [FK_MovieActor_Movie];
GO
IF OBJECT_ID(N'[dbo].[FK_MovieActor_Person]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[MovieActor] DROP CONSTRAINT [FK_MovieActor_Person];
GO
IF OBJECT_ID(N'[dbo].[FK_MovieDirector_Movie]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[MovieDirector] DROP CONSTRAINT [FK_MovieDirector_Movie];
GO
IF OBJECT_ID(N'[dbo].[FK_MovieDirector_Person]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[MovieDirector] DROP CONSTRAINT [FK_MovieDirector_Person];
GO
IF OBJECT_ID(N'[dbo].[FK_UserRememberedSessions]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[RememberedSessions] DROP CONSTRAINT [FK_UserRememberedSessions];
GO
IF OBJECT_ID(N'[dbo].[FK_UserToWatch]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[ToWatches] DROP CONSTRAINT [FK_UserToWatch];
GO
IF OBJECT_ID(N'[dbo].[FK_ToWatchMovie]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[ToWatches] DROP CONSTRAINT [FK_ToWatchMovie];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[Users]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Users];
GO
IF OBJECT_ID(N'[dbo].[Movies]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Movies];
GO
IF OBJECT_ID(N'[dbo].[MovieAtStorages]', 'U') IS NOT NULL
    DROP TABLE [dbo].[MovieAtStorages];
GO
IF OBJECT_ID(N'[dbo].[StorageLocations]', 'U') IS NOT NULL
    DROP TABLE [dbo].[StorageLocations];
GO
IF OBJECT_ID(N'[dbo].[SeenStatuses]', 'U') IS NOT NULL
    DROP TABLE [dbo].[SeenStatuses];
GO
IF OBJECT_ID(N'[dbo].[Genres]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Genres];
GO
IF OBJECT_ID(N'[dbo].[People]', 'U') IS NOT NULL
    DROP TABLE [dbo].[People];
GO
IF OBJECT_ID(N'[dbo].[RememberedSessions]', 'U') IS NOT NULL
    DROP TABLE [dbo].[RememberedSessions];
GO
IF OBJECT_ID(N'[dbo].[ToWatches]', 'U') IS NOT NULL
    DROP TABLE [dbo].[ToWatches];
GO
IF OBJECT_ID(N'[dbo].[GenreMovie]', 'U') IS NOT NULL
    DROP TABLE [dbo].[GenreMovie];
GO
IF OBJECT_ID(N'[dbo].[MovieWriter]', 'U') IS NOT NULL
    DROP TABLE [dbo].[MovieWriter];
GO
IF OBJECT_ID(N'[dbo].[MovieActor]', 'U') IS NOT NULL
    DROP TABLE [dbo].[MovieActor];
GO
IF OBJECT_ID(N'[dbo].[MovieDirector]', 'U') IS NOT NULL
    DROP TABLE [dbo].[MovieDirector];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'Users'
CREATE TABLE [dbo].[Users] (
    [Username] nvarchar(32)  NOT NULL,
    [Password] nvarchar(max)  NOT NULL,
    [LastLogin] datetime  NOT NULL,
    [Name] nvarchar(64)  NOT NULL,
    [IsAdmin] bit  NOT NULL
);
GO

-- Creating table 'Movies'
CREATE TABLE [dbo].[Movies] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Title] nvarchar(max)  NOT NULL,
    [Score] int  NOT NULL,
    [ImdbId] nvarchar(16)  NULL,
    [Plot] nvarchar(max)  NULL,
    [Year] smallint  NOT NULL,
    [ImdbQueried] bit  NOT NULL,
    [SeriesSeason] smallint  NULL,
    [SeriesEpisode] smallint  NULL,
    [ImageUrl] nvarchar(256)  NULL,
    [Runtime] int  NULL,
    [AddedAt] datetime  NOT NULL,
    [ImdbQueryFailCount] int  NOT NULL
);
GO

-- Creating table 'MovieAtStorages'
CREATE TABLE [dbo].[MovieAtStorages] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [RelativePath] nvarchar(512)  NOT NULL,
    [SampleRelativePath] nvarchar(512)  NULL,
    [CleanedName] nvarchar(64)  NOT NULL,
    [Ignore] bit  NOT NULL,
    [StorageLocation_Id] int  NOT NULL,
    [Movie_Id] int  NOT NULL
);
GO

-- Creating table 'StorageLocations'
CREATE TABLE [dbo].[StorageLocations] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(64)  NOT NULL,
    [Type] nvarchar(32)  NOT NULL,
    [Data1] nvarchar(max)  NOT NULL,
    [Data2] nvarchar(max)  NOT NULL,
    [Rebase] nvarchar(64)  NOT NULL,
    [Active] bit  NOT NULL
);
GO

-- Creating table 'SeenStatuses'
CREATE TABLE [dbo].[SeenStatuses] (
    [SeenAt] datetime  NOT NULL,
    [Review] nvarchar(max)  NOT NULL,
    [Score] int  NOT NULL,
    [Id] int IDENTITY(1,1) NOT NULL,
    [Movie_Id] int  NOT NULL,
    [User_Username] nvarchar(32)  NOT NULL
);
GO

-- Creating table 'Genres'
CREATE TABLE [dbo].[Genres] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(64)  NOT NULL
);
GO

-- Creating table 'People'
CREATE TABLE [dbo].[People] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'RememberedSessions'
CREATE TABLE [dbo].[RememberedSessions] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [UserUsername] nvarchar(32)  NOT NULL,
    [GuidHash] nvarchar(64)  NOT NULL,
    [CreatedAt] datetime  NOT NULL
);
GO

-- Creating table 'ToWatches'
CREATE TABLE [dbo].[ToWatches] (
    [UserUsername] nvarchar(32)  NOT NULL,
    [MovieId] int  NOT NULL,
    [Comment] nvarchar(500)  NOT NULL
);
GO

-- Creating table 'GenreMovie'
CREATE TABLE [dbo].[GenreMovie] (
    [Genres_Id] int  NOT NULL,
    [Movies_Id] int  NOT NULL
);
GO

-- Creating table 'MovieWriter'
CREATE TABLE [dbo].[MovieWriter] (
    [MoviesWhereWriter_Id] int  NOT NULL,
    [Writers_Id] int  NOT NULL
);
GO

-- Creating table 'MovieActor'
CREATE TABLE [dbo].[MovieActor] (
    [MoviesWhereActor_Id] int  NOT NULL,
    [Actors_Id] int  NOT NULL
);
GO

-- Creating table 'MovieDirector'
CREATE TABLE [dbo].[MovieDirector] (
    [MoviesWhereDirector_Id] int  NOT NULL,
    [Directors_Id] int  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Username] in table 'Users'
ALTER TABLE [dbo].[Users]
ADD CONSTRAINT [PK_Users]
    PRIMARY KEY CLUSTERED ([Username] ASC);
GO

-- Creating primary key on [Id] in table 'Movies'
ALTER TABLE [dbo].[Movies]
ADD CONSTRAINT [PK_Movies]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'MovieAtStorages'
ALTER TABLE [dbo].[MovieAtStorages]
ADD CONSTRAINT [PK_MovieAtStorages]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'StorageLocations'
ALTER TABLE [dbo].[StorageLocations]
ADD CONSTRAINT [PK_StorageLocations]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'SeenStatuses'
ALTER TABLE [dbo].[SeenStatuses]
ADD CONSTRAINT [PK_SeenStatuses]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Genres'
ALTER TABLE [dbo].[Genres]
ADD CONSTRAINT [PK_Genres]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'People'
ALTER TABLE [dbo].[People]
ADD CONSTRAINT [PK_People]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'RememberedSessions'
ALTER TABLE [dbo].[RememberedSessions]
ADD CONSTRAINT [PK_RememberedSessions]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [UserUsername], [MovieId] in table 'ToWatches'
ALTER TABLE [dbo].[ToWatches]
ADD CONSTRAINT [PK_ToWatches]
    PRIMARY KEY CLUSTERED ([UserUsername], [MovieId] ASC);
GO

-- Creating primary key on [Genres_Id], [Movies_Id] in table 'GenreMovie'
ALTER TABLE [dbo].[GenreMovie]
ADD CONSTRAINT [PK_GenreMovie]
    PRIMARY KEY NONCLUSTERED ([Genres_Id], [Movies_Id] ASC);
GO

-- Creating primary key on [MoviesWhereWriter_Id], [Writers_Id] in table 'MovieWriter'
ALTER TABLE [dbo].[MovieWriter]
ADD CONSTRAINT [PK_MovieWriter]
    PRIMARY KEY NONCLUSTERED ([MoviesWhereWriter_Id], [Writers_Id] ASC);
GO

-- Creating primary key on [MoviesWhereActor_Id], [Actors_Id] in table 'MovieActor'
ALTER TABLE [dbo].[MovieActor]
ADD CONSTRAINT [PK_MovieActor]
    PRIMARY KEY NONCLUSTERED ([MoviesWhereActor_Id], [Actors_Id] ASC);
GO

-- Creating primary key on [MoviesWhereDirector_Id], [Directors_Id] in table 'MovieDirector'
ALTER TABLE [dbo].[MovieDirector]
ADD CONSTRAINT [PK_MovieDirector]
    PRIMARY KEY NONCLUSTERED ([MoviesWhereDirector_Id], [Directors_Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [Movie_Id] in table 'SeenStatuses'
ALTER TABLE [dbo].[SeenStatuses]
ADD CONSTRAINT [FK_MovieSeenStatus]
    FOREIGN KEY ([Movie_Id])
    REFERENCES [dbo].[Movies]
        ([Id])
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_MovieSeenStatus'
CREATE INDEX [IX_FK_MovieSeenStatus]
ON [dbo].[SeenStatuses]
    ([Movie_Id]);
GO

-- Creating foreign key on [StorageLocation_Id] in table 'MovieAtStorages'
ALTER TABLE [dbo].[MovieAtStorages]
ADD CONSTRAINT [FK_StorageLocationMovieSource]
    FOREIGN KEY ([StorageLocation_Id])
    REFERENCES [dbo].[StorageLocations]
        ([Id])
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_StorageLocationMovieSource'
CREATE INDEX [IX_FK_StorageLocationMovieSource]
ON [dbo].[MovieAtStorages]
    ([StorageLocation_Id]);
GO

-- Creating foreign key on [User_Username] in table 'SeenStatuses'
ALTER TABLE [dbo].[SeenStatuses]
ADD CONSTRAINT [FK_UserSeenStatus]
    FOREIGN KEY ([User_Username])
    REFERENCES [dbo].[Users]
        ([Username])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_UserSeenStatus'
CREATE INDEX [IX_FK_UserSeenStatus]
ON [dbo].[SeenStatuses]
    ([User_Username]);
GO

-- Creating foreign key on [Movie_Id] in table 'MovieAtStorages'
ALTER TABLE [dbo].[MovieAtStorages]
ADD CONSTRAINT [FK_MovieMovieAtStorage]
    FOREIGN KEY ([Movie_Id])
    REFERENCES [dbo].[Movies]
        ([Id])
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_MovieMovieAtStorage'
CREATE INDEX [IX_FK_MovieMovieAtStorage]
ON [dbo].[MovieAtStorages]
    ([Movie_Id]);
GO

-- Creating foreign key on [Genres_Id] in table 'GenreMovie'
ALTER TABLE [dbo].[GenreMovie]
ADD CONSTRAINT [FK_GenreMovie_Genre]
    FOREIGN KEY ([Genres_Id])
    REFERENCES [dbo].[Genres]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Movies_Id] in table 'GenreMovie'
ALTER TABLE [dbo].[GenreMovie]
ADD CONSTRAINT [FK_GenreMovie_Movie]
    FOREIGN KEY ([Movies_Id])
    REFERENCES [dbo].[Movies]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_GenreMovie_Movie'
CREATE INDEX [IX_FK_GenreMovie_Movie]
ON [dbo].[GenreMovie]
    ([Movies_Id]);
GO

-- Creating foreign key on [MoviesWhereWriter_Id] in table 'MovieWriter'
ALTER TABLE [dbo].[MovieWriter]
ADD CONSTRAINT [FK_MovieWriter_Movie]
    FOREIGN KEY ([MoviesWhereWriter_Id])
    REFERENCES [dbo].[Movies]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Writers_Id] in table 'MovieWriter'
ALTER TABLE [dbo].[MovieWriter]
ADD CONSTRAINT [FK_MovieWriter_Person]
    FOREIGN KEY ([Writers_Id])
    REFERENCES [dbo].[People]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_MovieWriter_Person'
CREATE INDEX [IX_FK_MovieWriter_Person]
ON [dbo].[MovieWriter]
    ([Writers_Id]);
GO

-- Creating foreign key on [MoviesWhereActor_Id] in table 'MovieActor'
ALTER TABLE [dbo].[MovieActor]
ADD CONSTRAINT [FK_MovieActor_Movie]
    FOREIGN KEY ([MoviesWhereActor_Id])
    REFERENCES [dbo].[Movies]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Actors_Id] in table 'MovieActor'
ALTER TABLE [dbo].[MovieActor]
ADD CONSTRAINT [FK_MovieActor_Person]
    FOREIGN KEY ([Actors_Id])
    REFERENCES [dbo].[People]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_MovieActor_Person'
CREATE INDEX [IX_FK_MovieActor_Person]
ON [dbo].[MovieActor]
    ([Actors_Id]);
GO

-- Creating foreign key on [MoviesWhereDirector_Id] in table 'MovieDirector'
ALTER TABLE [dbo].[MovieDirector]
ADD CONSTRAINT [FK_MovieDirector_Movie]
    FOREIGN KEY ([MoviesWhereDirector_Id])
    REFERENCES [dbo].[Movies]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Directors_Id] in table 'MovieDirector'
ALTER TABLE [dbo].[MovieDirector]
ADD CONSTRAINT [FK_MovieDirector_Person]
    FOREIGN KEY ([Directors_Id])
    REFERENCES [dbo].[People]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_MovieDirector_Person'
CREATE INDEX [IX_FK_MovieDirector_Person]
ON [dbo].[MovieDirector]
    ([Directors_Id]);
GO

-- Creating foreign key on [UserUsername] in table 'RememberedSessions'
ALTER TABLE [dbo].[RememberedSessions]
ADD CONSTRAINT [FK_UserRememberedSessions]
    FOREIGN KEY ([UserUsername])
    REFERENCES [dbo].[Users]
        ([Username])
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_UserRememberedSessions'
CREATE INDEX [IX_FK_UserRememberedSessions]
ON [dbo].[RememberedSessions]
    ([UserUsername]);
GO

-- Creating foreign key on [UserUsername] in table 'ToWatches'
ALTER TABLE [dbo].[ToWatches]
ADD CONSTRAINT [FK_UserToWatch]
    FOREIGN KEY ([UserUsername])
    REFERENCES [dbo].[Users]
        ([Username])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating foreign key on [MovieId] in table 'ToWatches'
ALTER TABLE [dbo].[ToWatches]
ADD CONSTRAINT [FK_ToWatchMovie]
    FOREIGN KEY ([MovieId])
    REFERENCES [dbo].[Movies]
        ([Id])
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_ToWatchMovie'
CREATE INDEX [IX_FK_ToWatchMovie]
ON [dbo].[ToWatches]
    ([MovieId]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------