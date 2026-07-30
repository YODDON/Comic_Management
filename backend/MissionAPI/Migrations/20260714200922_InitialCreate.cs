using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MissionAPI.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[Missions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Missions] (
                    [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Missions] PRIMARY KEY,
                    [Title] nvarchar(max) NOT NULL, [Description] nvarchar(max) NOT NULL,
                    [RewardCoin] decimal(18,2) NOT NULL, [Type] nvarchar(max) NOT NULL,
                    [TargetCount] int NOT NULL, [IsActive] bit NOT NULL,
                    [StartDate] datetime2 NOT NULL, [EndDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL, [UpdatedAt] datetime2 NULL
                );
            END
            ELSE
            BEGIN
                IF COL_LENGTH(N'dbo.Missions', N'Type') IS NULL AND COL_LENGTH(N'dbo.Missions', N'MissionType') IS NOT NULL
                    EXEC sp_rename N'dbo.Missions.MissionType', N'Type', 'COLUMN';
                IF COL_LENGTH(N'dbo.Missions', N'TargetCount') IS NULL AND COL_LENGTH(N'dbo.Missions', N'TargetValue') IS NOT NULL
                    EXEC sp_rename N'dbo.Missions.TargetValue', N'TargetCount', 'COLUMN';
                IF COL_LENGTH(N'dbo.Missions', N'IsActive') IS NULL
                    ALTER TABLE [dbo].[Missions] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_Missions_IsActive] DEFAULT(1) WITH VALUES;
                IF COL_LENGTH(N'dbo.Missions', N'Status') IS NOT NULL
                BEGIN
                    EXEC(N'UPDATE [dbo].[Missions] SET [IsActive]=CASE WHEN [Status]=0 THEN 1 ELSE 0 END');
                    DECLARE @statusDefault sysname;
                    SELECT @statusDefault = dc.name
                    FROM sys.default_constraints dc
                    JOIN sys.columns c ON c.default_object_id=dc.object_id
                    WHERE c.object_id=OBJECT_ID(N'[dbo].[Missions]') AND c.name=N'Status';
                    IF @statusDefault IS NOT NULL EXEC(N'ALTER TABLE [dbo].[Missions] DROP CONSTRAINT [' + @statusDefault + N']');
                    ALTER TABLE [dbo].[Missions] DROP COLUMN [Status];
                END;
            END;

            IF OBJECT_ID(N'[dbo].[Notifications]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Notifications] (
                    [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Notifications] PRIMARY KEY,
                    [UserId] int NOT NULL, [Title] nvarchar(max) NOT NULL,
                    [Body] nvarchar(max) NOT NULL, [IsRead] bit NOT NULL,
                    [CreatedAt] datetime2 NOT NULL, [UpdatedAt] datetime2 NULL
                );
            END
            ELSE
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'[dbo].[Notifications]') AND name=N'UserId' AND system_type_id=36)
                BEGIN
                    EXEC sp_rename N'dbo.Notifications.UserId', N'LegacyUserId', 'COLUMN';
                    ALTER TABLE [dbo].[Notifications] ADD [UserId] int NOT NULL CONSTRAINT [DF_Notifications_UserId] DEFAULT(0) WITH VALUES;
                END;
                IF COL_LENGTH(N'dbo.Notifications', N'Body') IS NULL AND COL_LENGTH(N'dbo.Notifications', N'Description') IS NOT NULL
                    EXEC sp_rename N'dbo.Notifications.Description', N'Body', 'COLUMN';
                IF COL_LENGTH(N'dbo.Notifications', N'IsRead') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Notifications] ADD [IsRead] bit NOT NULL CONSTRAINT [DF_Notifications_IsRead] DEFAULT(0) WITH VALUES;
                    IF COL_LENGTH(N'dbo.Notifications', N'Status') IS NOT NULL
                        EXEC(N'UPDATE [dbo].[Notifications] SET [IsRead]=CASE WHEN [Status]=N''Read'' THEN 1 ELSE 0 END');
                END;
            END;

            IF OBJECT_ID(N'[dbo].[Uploads]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Uploads] (
                    [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Uploads] PRIMARY KEY,
                    [UserId] int NOT NULL, [FileType] nvarchar(max) NOT NULL,
                    [Url] nvarchar(max) NOT NULL, [FileSize] bigint NOT NULL,
                    [PublicId] nvarchar(max) NOT NULL, [CreatedAt] datetime2 NOT NULL, [UpdatedAt] datetime2 NULL
                );
            END
            ELSE
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'[dbo].[Uploads]') AND name=N'UserId' AND system_type_id=36)
                BEGIN
                    EXEC sp_rename N'dbo.Uploads.UserId', N'LegacyUserId', 'COLUMN';
                    ALTER TABLE [dbo].[Uploads] ADD [UserId] int NOT NULL CONSTRAINT [DF_Uploads_UserId] DEFAULT(0) WITH VALUES;
                END;
                IF COL_LENGTH(N'dbo.Uploads', N'FileSize') IS NULL ALTER TABLE [dbo].[Uploads] ADD [FileSize] bigint NOT NULL CONSTRAINT [DF_Uploads_FileSize] DEFAULT(0) WITH VALUES;
                IF COL_LENGTH(N'dbo.Uploads', N'PublicId') IS NULL ALTER TABLE [dbo].[Uploads] ADD [PublicId] nvarchar(max) NOT NULL CONSTRAINT [DF_Uploads_PublicId] DEFAULT(N'') WITH VALUES;
            END;

            IF OBJECT_ID(N'[dbo].[UserMissions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[UserMissions] (
                    [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_UserMissions] PRIMARY KEY,
                    [UserId] int NOT NULL, [MissionId] uniqueidentifier NOT NULL,
                    [CurrentProgress] int NOT NULL, [IsCompleted] bit NOT NULL,
                    [CompletedAt] datetime2 NULL, [CreatedAt] datetime2 NOT NULL, [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [FK_UserMissions_Missions_MissionId] FOREIGN KEY([MissionId]) REFERENCES [dbo].[Missions]([Id]) ON DELETE CASCADE
                );
            END
            ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'[dbo].[UserMissions]') AND name=N'UserId' AND system_type_id=36)
            BEGIN
                EXEC sp_rename N'dbo.UserMissions.UserId', N'LegacyUserId', 'COLUMN';
                ALTER TABLE [dbo].[UserMissions] ADD [UserId] int NOT NULL CONSTRAINT [DF_UserMissions_UserId] DEFAULT(0) WITH VALUES;
            END;

            IF COL_LENGTH(N'dbo.UserMissions', N'CreatedAt') IS NULL
                ALTER TABLE [dbo].[UserMissions] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_UserMissions_CreatedAt] DEFAULT(SYSUTCDATETIME()) WITH VALUES;
            IF COL_LENGTH(N'dbo.UserMissions', N'UpdatedAt') IS NULL
                ALTER TABLE [dbo].[UserMissions] ADD [UpdatedAt] datetime2 NULL;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_UserMissions_MissionId' AND object_id=OBJECT_ID(N'[dbo].[UserMissions]'))
                CREATE INDEX [IX_UserMissions_MissionId] ON [dbo].[UserMissions]([MissionId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_UserMissions_UserId_MissionId' AND object_id=OBJECT_ID(N'[dbo].[UserMissions]'))
                CREATE UNIQUE INDEX [IX_UserMissions_UserId_MissionId] ON [dbo].[UserMissions]([UserId],[MissionId]);

            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) { }
}
