using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerAPI.Migrations
{
    /// <inheritdoc />
    public partial class EnsureBannerTargetUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[Banners]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.Banners', N'TargetUrl') IS NULL
                BEGIN
                    IF COL_LENGTH(N'dbo.Banners', N'LinkUrl') IS NOT NULL
                        EXEC sp_rename N'[dbo].[Banners].[LinkUrl]', N'TargetUrl', N'COLUMN';
                    ELSE
                        ALTER TABLE [dbo].[Banners]
                        ADD [TargetUrl] nvarchar(2048) NOT NULL
                            CONSTRAINT [DF_Banners_TargetUrl] DEFAULT (N'') WITH VALUES;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left empty. The column may contain legacy banner links,
            // so rolling back this compatibility migration must not delete data.
        }
    }
}
