using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MissionAPI.Data;

#nullable disable

namespace MissionAPI.Migrations;

[DbContext(typeof(MissionDbContext))]
[Migration("20260716000100_RepairLegacyUserMissionAuditColumns")]
public class RepairLegacyUserMissionAuditColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[UserMissions]', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.UserMissions', N'CreatedAt') IS NULL
                    ALTER TABLE [dbo].[UserMissions]
                    ADD [CreatedAt] datetime2 NOT NULL
                        CONSTRAINT [DF_UserMissions_CreatedAt] DEFAULT(SYSUTCDATETIME()) WITH VALUES;

                IF COL_LENGTH(N'dbo.UserMissions', N'UpdatedAt') IS NULL
                    ALTER TABLE [dbo].[UserMissions] ADD [UpdatedAt] datetime2 NULL;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Compatibility repair: audit columns may predate this migration, so do not remove them.
    }
}
