using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChapterAPI.Migrations
{
    /// <inheritdoc />
    public partial class AlignPurchaseUserIdWithUserApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserPurchases_UserId_ChapterId",
                table: "UserPurchases");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "UserPurchases",
                newName: "LegacyUserId");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "UserPurchases",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPurchases_LegacyUserId_ChapterId",
                table: "UserPurchases",
                columns: new[] { "LegacyUserId", "ChapterId" },
                unique: true,
                filter: "[LegacyUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserPurchases_UserId_ChapterId",
                table: "UserPurchases",
                columns: new[] { "UserId", "ChapterId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserPurchases_LegacyUserId_ChapterId",
                table: "UserPurchases");

            migrationBuilder.DropIndex(
                name: "IX_UserPurchases_UserId_ChapterId",
                table: "UserPurchases");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserPurchases");

            migrationBuilder.RenameColumn(
                name: "LegacyUserId",
                table: "UserPurchases",
                newName: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPurchases_UserId_ChapterId",
                table: "UserPurchases",
                columns: new[] { "UserId", "ChapterId" },
                unique: true);
        }
    }
}
