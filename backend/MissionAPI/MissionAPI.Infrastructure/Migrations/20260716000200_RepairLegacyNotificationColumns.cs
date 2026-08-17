using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MissionAPI.Data;

#nullable disable

namespace MissionAPI.Migrations;

[DbContext(typeof(MissionDbContext))]
[Migration("20260716000200_RepairLegacyNotificationColumns")]
public class RepairLegacyNotificationColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[Notifications]', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.Notifications', N'CreatedAt') IS NULL
                    ALTER TABLE [dbo].[Notifications] ADD [CreatedAt] datetime2 NOT NULL
                        CONSTRAINT [DF_Notifications_CreatedAt] DEFAULT(SYSUTCDATETIME()) WITH VALUES;
                IF COL_LENGTH(N'dbo.Notifications', N'UpdatedAt') IS NULL
                    ALTER TABLE [dbo].[Notifications] ADD [UpdatedAt] datetime2 NULL;
                IF COL_LENGTH(N'dbo.Notifications', N'Body') IS NULL
                    ALTER TABLE [dbo].[Notifications] ADD [Body] nvarchar(max) NOT NULL
                        CONSTRAINT [DF_Notifications_Body] DEFAULT(N'') WITH VALUES;
                IF COL_LENGTH(N'dbo.Notifications', N'IsRead') IS NULL
                    ALTER TABLE [dbo].[Notifications] ADD [IsRead] bit NOT NULL
                        CONSTRAINT [DF_Notifications_IsRead_Repair] DEFAULT(0) WITH VALUES;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) { }
}
