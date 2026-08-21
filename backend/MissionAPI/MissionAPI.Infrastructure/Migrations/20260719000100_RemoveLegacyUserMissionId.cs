using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MissionAPI.Data;

#nullable disable

namespace MissionAPI.Migrations;

[DbContext(typeof(MissionDbContext))]
[Migration("20260719000100_RemoveLegacyUserMissionId")]
public class RemoveLegacyUserMissionId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[UserMissions]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.UserMissions', N'LegacyUserId') IS NOT NULL
            BEGIN
                DECLARE @defaultConstraint sysname;
                SELECT @defaultConstraint = dc.[name]
                FROM sys.default_constraints dc
                INNER JOIN sys.columns c
                    ON c.object_id = dc.parent_object_id
                    AND c.column_id = dc.parent_column_id
                WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[UserMissions]')
                  AND c.[name] = N'LegacyUserId';

                IF @defaultConstraint IS NOT NULL
                    EXEC(N'ALTER TABLE [dbo].[UserMissions] DROP CONSTRAINT [' + @defaultConstraint + N']');

                ALTER TABLE [dbo].[UserMissions] DROP COLUMN [LegacyUserId];
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The legacy GUID cannot be reconstructed from the current numeric UserId.
    }
}
