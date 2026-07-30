using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletAPI.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeWalletSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Do not silently discard the only identifier of a legacy wallet. Such rows require
            // an explicit Guid-to-int user mapping before this cleanup can be deployed.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [dbo].[Wallets] WHERE [UserId] IS NULL)
                    THROW 51000, 'Cannot remove LegacyUserId while wallets without a current UserId exist.', 1;
                """);

            // These are empty legacy tables and are not represented by the current model.
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS [dbo].[CurrencyLedger];
                DROP TABLE IF EXISTS [dbo].[WithdrawRequests];
                """);

            migrationBuilder.DropIndex(
                name: "IX_Wallets_LegacyUserId",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "LegacyUserId",
                table: "Wallets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The obsolete tables are intentionally not recreated. They were empty and their
            // schemas are not part of the current WalletAPI model.
            migrationBuilder.AddColumn<Guid>(
                name: "LegacyUserId",
                table: "Wallets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_LegacyUserId",
                table: "Wallets",
                column: "LegacyUserId",
                unique: true,
                filter: "[LegacyUserId] IS NOT NULL");
        }
    }
}
