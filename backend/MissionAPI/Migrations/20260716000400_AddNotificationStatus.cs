using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MissionAPI.Data;

#nullable disable
namespace MissionAPI.Migrations;

[DbContext(typeof(MissionDbContext))]
[Migration("20260716000400_AddNotificationStatus")]
public class AddNotificationStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        IF COL_LENGTH(N'dbo.Notifications', N'IsActive') IS NULL
            ALTER TABLE [dbo].[Notifications] ADD [IsActive] bit NOT NULL
                CONSTRAINT [DF_Notifications_IsActive] DEFAULT(1) WITH VALUES;
        """);
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
