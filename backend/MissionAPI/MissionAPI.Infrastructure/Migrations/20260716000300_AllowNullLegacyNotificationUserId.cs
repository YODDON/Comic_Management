using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MissionAPI.Data;

#nullable disable

namespace MissionAPI.Migrations;

[DbContext(typeof(MissionDbContext))]
[Migration("20260716000300_AllowNullLegacyNotificationUserId")]
public class AllowNullLegacyNotificationUserId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Notifications', N'LegacyUserId') IS NOT NULL
                ALTER TABLE [dbo].[Notifications] ALTER COLUMN [LegacyUserId] uniqueidentifier NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) { }
}
