using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MissionAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMissionActivityTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MissionActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionActivities_Missions_MissionId",
                        column: x => x.MissionId,
                        principalTable: "Missions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MissionActivities_MissionId",
                table: "MissionActivities",
                column: "MissionId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionActivities_UserId_MissionId_ActivityId",
                table: "MissionActivities",
                columns: new[] { "UserId", "MissionId", "ActivityId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MissionActivities");

        }
    }
}
