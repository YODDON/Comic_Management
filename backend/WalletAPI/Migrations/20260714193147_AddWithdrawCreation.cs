using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddWithdrawCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Withdraws_WalletId",
                table: "Withdraws");

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Withdraws",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Withdraws_WalletId_Pending",
                table: "Withdraws",
                column: "WalletId",
                unique: true,
                filter: "[Status] = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Withdraws_WalletId_Pending",
                table: "Withdraws");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Withdraws");

            migrationBuilder.CreateIndex(
                name: "IX_Withdraws_WalletId",
                table: "Withdraws",
                column: "WalletId");
        }
    }
}
