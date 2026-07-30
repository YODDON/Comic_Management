using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletAPI.Migrations
{
    /// <inheritdoc />
    public partial class AlignWalletUserIdWithUserApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Wallets",
                newName: "LegacyUserId");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Wallets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_LegacyUserId",
                table: "Wallets",
                column: "LegacyUserId",
                unique: true,
                filter: "[LegacyUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallets_LegacyUserId",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Wallets");

            migrationBuilder.RenameColumn(
                name: "LegacyUserId",
                table: "Wallets",
                newName: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets",
                column: "UserId",
                unique: true);
        }
    }
}
