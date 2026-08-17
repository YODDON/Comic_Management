using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChapterAPI.Migrations
{
    /// <inheritdoc />
    public partial class NormalizePurchaseUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A legacy Guid cannot be inferred as the current integer UserAPI id. Stop instead of
            // silently assigning an invalid user when historical purchases still need migration.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [dbo].[UserPurchases] WHERE [UserId] IS NULL)
                    THROW 51010, 'Map legacy chapter purchases to current UserId values before normalizing.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_UserPurchases_LegacyUserId_ChapterId",
                table: "UserPurchases");

            migrationBuilder.DropIndex(
                name: "IX_UserPurchases_UserId_ChapterId",
                table: "UserPurchases");

            migrationBuilder.DropColumn(
                name: "LegacyUserId",
                table: "UserPurchases");

            migrationBuilder.Sql(
                "ALTER TABLE [dbo].[UserPurchases] ALTER COLUMN [UserId] int NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_UserPurchases_UserId_ChapterId",
                table: "UserPurchases",
                columns: new[] { "UserId", "ChapterId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserPurchases_UserId_ChapterId",
                table: "UserPurchases");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserPurchases",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "LegacyUserId",
                table: "UserPurchases",
                type: "uniqueidentifier",
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
    }
}
