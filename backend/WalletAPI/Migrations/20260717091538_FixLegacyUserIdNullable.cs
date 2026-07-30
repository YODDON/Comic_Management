using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletAPI.Migrations
{
    /// <summary>
    /// Repairs schema drift: Wallets.LegacyUserId is NOT NULL in the database but nullable in the
    /// model, so inserting a wallet fails with "Cannot insert the value NULL into column
    /// 'LegacyUserId'". That broke AddCoin for every user who did not already have a wallet row.
    /// </summary>
    /// <remarks>
    /// Cause: AlignWalletUserIdWithUserApi renamed UserId (Guid, NOT NULL) to LegacyUserId. A rename
    /// carries the old nullability over, and no AlterColumn was emitted to drop it. EF compares the
    /// model against its snapshot rather than the live database, and the snapshot already said
    /// nullable, so `migrations add` produced an empty migration and the drift went unnoticed.
    ///
    /// The body below is written by hand for that reason — scaffolding cannot see the discrepancy.
    /// </remarks>
    public partial class FixLegacyUserIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server cannot alter a column while an index references it.
            migrationBuilder.DropIndex(
                name: "IX_Wallets_LegacyUserId",
                table: "Wallets");

            migrationBuilder.AlterColumn<Guid>(
                name: "LegacyUserId",
                table: "Wallets",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_LegacyUserId",
                table: "Wallets",
                column: "LegacyUserId",
                unique: true,
                filter: "[LegacyUserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only reversible while no wallet has a NULL LegacyUserId. Once a user created after this
            // migration exists, rolling back fails on the NOT NULL constraint — which is correct:
            // those rows have no legacy id to restore.
            migrationBuilder.DropIndex(
                name: "IX_Wallets_LegacyUserId",
                table: "Wallets");

            migrationBuilder.AlterColumn<Guid>(
                name: "LegacyUserId",
                table: "Wallets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_LegacyUserId",
                table: "Wallets",
                column: "LegacyUserId",
                unique: true,
                filter: "[LegacyUserId] IS NOT NULL");
        }
    }
}
