using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Older versions created this table with EnsureCreated(), so the table may
            // exist without an entry in __EFMigrationsHistory. Upgrade it in place.
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[Banners]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[Banners] (
                        [Id] uniqueidentifier NOT NULL,
                        [Title] nvarchar(200) NOT NULL,
                        [ImageUrl] nvarchar(2048) NOT NULL,
                        [ImagePublicId] nvarchar(500) NULL,
                        [TargetUrl] nvarchar(2048) NOT NULL,
                        [IsActive] bit NOT NULL,
                        [DisplayOrder] int NOT NULL CONSTRAINT [DF_Banners_DisplayOrder] DEFAULT (0),
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        CONSTRAINT [PK_Banners] PRIMARY KEY ([Id])
                    );
                END
                ELSE
                BEGIN
                    IF COL_LENGTH(N'dbo.Banners', N'ImagePublicId') IS NULL
                        ALTER TABLE [dbo].[Banners] ADD [ImagePublicId] nvarchar(500) NULL;

                    IF COL_LENGTH(N'dbo.Banners', N'DisplayOrder') IS NULL
                        ALTER TABLE [dbo].[Banners] ADD [DisplayOrder] int NOT NULL
                            CONSTRAINT [DF_Banners_DisplayOrder] DEFAULT (0) WITH VALUES;
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_Banners_IsActive_DisplayOrder'
                      AND [object_id] = OBJECT_ID(N'[dbo].[Banners]'))
                BEGIN
                    CREATE INDEX [IX_Banners_IsActive_DisplayOrder]
                        ON [dbo].[Banners] ([IsActive], [DisplayOrder]);
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep legacy banner data intact when rolling back this baseline migration.
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_Banners_IsActive_DisplayOrder'
                      AND [object_id] = OBJECT_ID(N'[dbo].[Banners]'))
                    DROP INDEX [IX_Banners_IsActive_DisplayOrder] ON [dbo].[Banners];
                """);
        }
    }
}
